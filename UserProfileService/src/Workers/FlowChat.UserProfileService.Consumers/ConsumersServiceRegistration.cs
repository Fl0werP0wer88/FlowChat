using Confluent.Kafka;
using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.UserProfileService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(UserCreatedConsumerOptions.SectionName)
            .Get<UserCreatedConsumerOptions>()
            ?? new UserCreatedConsumerOptions();
        var createdProducerOptions = configuration
            .GetSection(UserProfileCreatedProducerOptions.SectionName)
            .Get<UserProfileCreatedProducerOptions>()
            ?? new UserProfileCreatedProducerOptions();
        var stateChangedProducerOptions = configuration
            .GetSection(UserProfileStateChangedProducerOptions.SectionName)
            .Get<UserProfileStateChangedProducerOptions>()
            ?? new UserProfileStateChangedProducerOptions();
        var bootstrapServers = !string.IsNullOrWhiteSpace(createdProducerOptions.BootstrapServers)
            ? createdProducerOptions.BootstrapServers
            : !string.IsNullOrWhiteSpace(stateChangedProducerOptions.BootstrapServers)
                ? stateChangedProducerOptions.BootstrapServers
                : consumerOptions.BootstrapServers;

        services.AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<UserProfileCreatedIntegrationEvent>("user-profile-created", endpoint => endpoint
                            .ProduceTo(createdProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserProfileStateChangedIntegrationEvent>("user-profile-state-changed", endpoint => endpoint
                            .ProduceTo(stateChangedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(consumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(consumerOptions.Topic)
                            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
                            .IgnoreUnhandledMessages()
                            .OnError(policy =>
                            {
                                policy.MoveTo(consumerOptions.DeadLetterTopic, move => move
                                    .ApplyTo<InvalidOperationException>());

                                policy.Retry(retry => retry
                                        .WithMaxRetries(consumerOptions.MaxRetryCount)
                                        .Exclude<InvalidOperationException>()
                                        .WithExponentialDelay(
                                            TimeSpan.FromSeconds(consumerOptions.RetryBaseDelaySeconds),
                                            2,
                                            TimeSpan.FromSeconds(consumerOptions.RetryMaxDelaySeconds)))
                                    .ThenMoveTo(consumerOptions.DeadLetterTopic, move => move
                                        .Exclude<InvalidOperationException>());
                            })));
            })
            .AddScopedSubscriber<UserCreatedSubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}

using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.NotificationService.Application.Notifications.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Persistence;
using FlowChat.NotificationService.Worker.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.NotificationService.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration.GetSection(UserCreatedConsumerOptions.SectionName)
            .Get<UserCreatedConsumerOptions>() ?? new UserCreatedConsumerOptions();

        services.Configure<UserCreatedConsumerOptions>(
            configuration.GetSection(UserCreatedConsumerOptions.SectionName));

        services.AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkKafkaOffsetStore();
            })
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(consumerOptions.BootstrapServers)
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
            .AddScopedSubscriber<UserEmailVerificationRequestedSubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}

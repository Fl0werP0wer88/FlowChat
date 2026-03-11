using Confluent.Kafka;
using FlowChat.SocialGraphService.Worker.Configuration;
using FlowChat.SocialGraphService.Worker.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.SocialGraphService.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new WorkerSettingsManager(configuration);
        services.TryAddSingleton<IWorkerSettingsManager>(settingsManager);
        var userCreatedConsumerOptions = settingsManager.GetUserCreatedConsumerOptions();
        var userProfileConsumerOptions = settingsManager.GetUserProfileConsumerOptions();
        services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(userCreatedConsumerOptions, userProfileConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userCreatedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userCreatedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(userCreatedConsumerOptions.Topic)
                            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
                            .IgnoreUnhandledMessages()
                            .OnError(policy =>
                            {
                                policy.MoveTo(userCreatedConsumerOptions.DeadLetterTopic, move => move
                                    .ApplyTo<InvalidOperationException>());

                                policy.Retry(retry => retry
                                        .WithMaxRetries(userCreatedConsumerOptions.MaxRetryCount)
                                        .Exclude<InvalidOperationException>()
                                        .WithExponentialDelay(
                                            TimeSpan.FromSeconds(userCreatedConsumerOptions.RetryBaseDelaySeconds),
                                            2,
                                            TimeSpan.FromSeconds(userCreatedConsumerOptions.RetryMaxDelaySeconds)))
                                    .ThenMoveTo(userCreatedConsumerOptions.DeadLetterTopic, move => move
                                        .Exclude<InvalidOperationException>());
                            })))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userProfileConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userProfileConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(userProfileConsumerOptions.Topic)
                            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
                            .IgnoreUnhandledMessages()
                            .OnError(policy =>
                            {
                                policy.MoveTo(userProfileConsumerOptions.DeadLetterTopic, move => move
                                    .ApplyTo<InvalidOperationException>());

                                policy.Retry(retry => retry
                                        .WithMaxRetries(userProfileConsumerOptions.MaxRetryCount)
                                        .Exclude<InvalidOperationException>()
                                        .WithExponentialDelay(
                                            TimeSpan.FromSeconds(userProfileConsumerOptions.RetryBaseDelaySeconds),
                                            2,
                                            TimeSpan.FromSeconds(userProfileConsumerOptions.RetryMaxDelaySeconds)))
                                    .ThenMoveTo(userProfileConsumerOptions.DeadLetterTopic, move => move
                                        .Exclude<InvalidOperationException>());
                            })));
            })
            .AddScopedSubscriber<UserCreatedSubscriber>()
            .AddScopedSubscriber<UserProfileSubscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        UserCreatedConsumerOptions userCreatedConsumerOptions,
        UserProfileConsumerOptions userProfileConsumerOptions) =>
        !string.IsNullOrWhiteSpace(userCreatedConsumerOptions.BootstrapServers)
            ? userCreatedConsumerOptions.BootstrapServers
            : userProfileConsumerOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}

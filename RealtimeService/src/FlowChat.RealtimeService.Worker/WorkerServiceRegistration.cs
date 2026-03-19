using Confluent.Kafka;
using FlowChat.RealtimeService.Worker.Configuration;
using FlowChat.RealtimeService.Worker.Kafka;
using FlowChat.RealtimeService.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.RealtimeService.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IWorkerSettingsManager>(new WorkerSettingsManager(configuration));
        services.AddHttpClient<IRealtimeInternalApiClient, RealtimeInternalApiClient>();

        return services;
    }

    public static IServiceCollection AddWorkerKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new WorkerSettingsManager(configuration);
        services.TryAddSingleton<IWorkerSettingsManager>(settingsManager);
        var chatMessageSentConsumerOptions = settingsManager.GetChatMessageSentConsumerOptions();
        var userPresenceChangedConsumerOptions = settingsManager.GetUserPresenceChangedConsumerOptions();

        services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(chatMessageSentConsumerOptions, userPresenceChangedConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(chatMessageSentConsumerOptions.Topic)
                            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
                            .IgnoreUnhandledMessages()
                            .OnError(policy =>
                            {
                                policy.MoveTo(chatMessageSentConsumerOptions.DeadLetterTopic, move => move
                                    .ApplyTo<InvalidOperationException>());

                                policy.Retry(retry => retry
                                        .WithMaxRetries(chatMessageSentConsumerOptions.MaxRetryCount)
                                        .Exclude<InvalidOperationException>()
                                        .WithExponentialDelay(
                                            TimeSpan.FromSeconds(chatMessageSentConsumerOptions.RetryBaseDelaySeconds),
                                            2,
                                            TimeSpan.FromSeconds(chatMessageSentConsumerOptions.RetryMaxDelaySeconds)))
                                    .ThenMoveTo(chatMessageSentConsumerOptions.DeadLetterTopic, move => move
                                        .Exclude<InvalidOperationException>());
                            })))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userPresenceChangedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userPresenceChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(userPresenceChangedConsumerOptions.Topic)
                            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
                            .IgnoreUnhandledMessages()
                            .OnError(policy =>
                            {
                                policy.MoveTo(userPresenceChangedConsumerOptions.DeadLetterTopic, move => move
                                    .ApplyTo<InvalidOperationException>());

                                policy.Retry(retry => retry
                                        .WithMaxRetries(userPresenceChangedConsumerOptions.MaxRetryCount)
                                        .Exclude<InvalidOperationException>()
                                        .WithExponentialDelay(
                                            TimeSpan.FromSeconds(userPresenceChangedConsumerOptions.RetryBaseDelaySeconds),
                                            2,
                                            TimeSpan.FromSeconds(userPresenceChangedConsumerOptions.RetryMaxDelaySeconds)))
                                    .ThenMoveTo(userPresenceChangedConsumerOptions.DeadLetterTopic, move => move
                                        .Exclude<InvalidOperationException>());
                            })));
            })
            .AddScopedSubscriber<ChatMessageSentSubscriber>()
            .AddScopedSubscriber<UserPresenceChangedSubscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        ChatMessageSentConsumerOptions chatMessageSentConsumerOptions,
        UserPresenceChangedConsumerOptions userPresenceChangedConsumerOptions) =>
        !string.IsNullOrWhiteSpace(chatMessageSentConsumerOptions.BootstrapServers)
            ? chatMessageSentConsumerOptions.BootstrapServers
            : userPresenceChangedConsumerOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}

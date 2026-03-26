using Confluent.Kafka;
using FlowChat.Core.Exceptions;
using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.RealtimeService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new ConsumersSettingsManager(configuration);
        services.TryAddSingleton<IConsumersSettingsManager>(settingsManager);

        var chatMessageSentConsumerOptions = settingsManager.GetChatMessageSentConsumerOptions();
        var userPresenceChangedConsumerOptions = settingsManager.GetUserPresenceChangedConsumerOptions();

        services.AddHttpClient<IRealtimeInternalApiClient, RealtimeInternalApiClient>();

        services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(chatMessageSentConsumerOptions, userPresenceChangedConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => ConfigureMainEndpoint(
                            endpoint,
                            chatMessageSentConsumerOptions.Topic,
                            chatMessageSentConsumerOptions.RetryTopic,
                            chatMessageSentConsumerOptions.DeadLetterTopic)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => ConfigureRetryEndpoint(
                            endpoint,
                            chatMessageSentConsumerOptions.RetryTopic,
                            chatMessageSentConsumerOptions.DeadLetterTopic,
                            chatMessageSentConsumerOptions.MaxRetryCount,
                            chatMessageSentConsumerOptions.RetryBaseDelaySeconds,
                            chatMessageSentConsumerOptions.RetryMaxDelaySeconds)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userPresenceChangedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userPresenceChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => ConfigureMainEndpoint(
                            endpoint,
                            userPresenceChangedConsumerOptions.Topic,
                            userPresenceChangedConsumerOptions.RetryTopic,
                            userPresenceChangedConsumerOptions.DeadLetterTopic)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userPresenceChangedConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userPresenceChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => ConfigureRetryEndpoint(
                            endpoint,
                            userPresenceChangedConsumerOptions.RetryTopic,
                            userPresenceChangedConsumerOptions.DeadLetterTopic,
                            userPresenceChangedConsumerOptions.MaxRetryCount,
                            userPresenceChangedConsumerOptions.RetryBaseDelaySeconds,
                            userPresenceChangedConsumerOptions.RetryMaxDelaySeconds)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageSentConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageSentConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(userPresenceChangedConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(userPresenceChangedConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
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

    private static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureMainEndpoint(
        KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string topic,
        string retryTopic,
        string deadLetterTopic) =>
        ConfigureEndpointDefaults(endpoint, topic)
            .OnError(policy =>
            {
                policy.MoveTo(deadLetterTopic, move => move
                    .ApplyTo<NonTransientException>());

                policy.MoveTo(retryTopic, move => move
                    .Exclude<NonTransientException>());
            });

    private static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureRetryEndpoint(
        KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string retryTopic,
        string deadLetterTopic,
        int maxRetryCount,
        int retryBaseDelaySeconds,
        int retryMaxDelaySeconds) =>
        ConfigureEndpointDefaults(endpoint, retryTopic)
            .OnError(policy =>
            {
                policy.MoveTo(deadLetterTopic, move => move
                    .ApplyTo<NonTransientException>());

                policy.Retry(retry => retry
                        .WithMaxRetries(maxRetryCount)
                        .Exclude<NonTransientException>()
                        .WithExponentialDelay(
                            TimeSpan.FromSeconds(retryBaseDelaySeconds),
                            2,
                            TimeSpan.FromSeconds(retryMaxDelaySeconds)))
                    .ThenMoveTo(deadLetterTopic, move => move
                        .Exclude<NonTransientException>());
            });

    private static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureEndpointDefaults(
        KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string topic) =>
        endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
            .IgnoreUnhandledMessages();
}

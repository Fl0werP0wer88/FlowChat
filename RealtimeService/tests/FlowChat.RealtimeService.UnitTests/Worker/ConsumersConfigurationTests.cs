using System.Collections;
using FlowChat.RealtimeService.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConsumersConfigurationTests
{
    [Fact]
    public void AddConsumers_RegistersMainAndRetryConsumersForBothTopics()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RealtimeApi:BaseUrl"] = "http://localhost:5215",
                ["RealtimeApi:ApiKey"] = "worker-key",
                ["Kafka:ChatMessageSentConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:ChatMessageSentConsumer:GroupId"] = "realtime-service",
                ["Kafka:ChatMessageSentConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ChatMessageSentConsumer:Topic"] = "dev.flowchat.chat.message.v1",
                ["Kafka:ChatMessageSentConsumer:RetryTopic"] = "dev.flowchat.chat.message.v1.retry",
                ["Kafka:ChatMessageSentConsumer:DeadLetterTopic"] = "dev.flowchat.chat.message.v1.dlq",
                ["Kafka:ChatMessageSentConsumer:MaxRetryCount"] = "5",
                ["Kafka:ChatMessageSentConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:ChatMessageSentConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:ChatMessageSentConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:UserPresenceChangedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserPresenceChangedConsumer:GroupId"] = "realtime-service",
                ["Kafka:UserPresenceChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:UserPresenceChangedConsumer:Topic"] = "dev.flowchat.user-profile.presence-changed.v1",
                ["Kafka:UserPresenceChangedConsumer:RetryTopic"] = "dev.flowchat.user-profile.presence-changed.v1.retry",
                ["Kafka:UserPresenceChangedConsumer:DeadLetterTopic"] = "dev.flowchat.user-profile.presence-changed.v1.dlq",
                ["Kafka:UserPresenceChangedConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserPresenceChangedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserPresenceChangedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:UserPresenceChangedConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var consumers = Assert.IsAssignableFrom<IEnumerable>(consumerCollection)
            .Cast<object>()
            .ToList();

        Assert.Equal(4, consumers.Count);

        var configuredTopics = consumers
            .SelectMany(GetConfiguredTopics)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(topic => topic, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "dev.flowchat.chat.message.v1",
                "dev.flowchat.chat.message.v1.retry",
                "dev.flowchat.user-profile.presence-changed.v1",
                "dev.flowchat.user-profile.presence-changed.v1.retry"
            ],
            configuredTopics);
    }

    private static IEnumerable<string> GetConfiguredTopics(object consumer)
    {
        var endpointsConfiguration = Assert.IsAssignableFrom<IEnumerable>(
            consumer.GetType().GetProperty("EndpointsConfiguration")!.GetValue(consumer));

        foreach (var endpoint in endpointsConfiguration.Cast<object>())
        {
            var topicPartitions = Assert.IsAssignableFrom<IEnumerable>(
                endpoint.GetType().GetProperty("TopicPartitions")!.GetValue(endpoint));

            foreach (var topicPartition in topicPartitions.Cast<object>())
            {
                var topic = topicPartition.GetType().GetProperty("Topic")!.GetValue(topicPartition)?.ToString();

                if (!string.IsNullOrWhiteSpace(topic))
                {
                    yield return topic;
                }
            }
        }
    }
}

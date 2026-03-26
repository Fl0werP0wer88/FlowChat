using System.Collections;
using FlowChat.NotificationService.Consumers;
using FlowChat.NotificationService.Consumers.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.NotificationService.UnitTests;

public sealed class NotificationConsumerConfigurationTests
{
    [Fact]
    public void AddConsumers_RegistersMainAndRetryConsumers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotificationApi:BaseUrl"] = "https://localhost:7206",
                ["NotificationApi:ApiKey"] = "worker-key",
                ["Kafka:UserEmailVerificationRequestedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailVerificationRequestedConsumer:GroupId"] = "notification-service",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryGroupId"] = "notification-service-retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:Topic"] = "dev.flowchat.notification.email.v1",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTopic"] = "dev.flowchat.notification.email.v1.retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:DeadLetterTopic"] = "dev.flowchat.notification.email.v1.dlq",
                ["Kafka:UserEmailVerificationRequestedConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:UserEmailVerificationRequestedConsumer:AutoOffsetReset"] = "Earliest"
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

        Assert.Equal(2, consumers.Count);

        var configuredTopics = consumers
            .SelectMany(GetConfiguredTopics)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(topic => topic, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "dev.flowchat.notification.email.v1",
                "dev.flowchat.notification.email.v1.retry"
            ],
            configuredTopics);
    }

    [Theory]
    [InlineData("NotificationService/src/Workers/FlowChat.NotificationService.Consumers/appsettings.json")]
    [InlineData("NotificationService/src/Workers/FlowChat.NotificationService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(UserEmailVerificationRequestedConsumerOptions.SectionName)
            .Get<UserEmailVerificationRequestedConsumerOptions>();

        Assert.NotNull(consumerOptions);
        Assert.Equal("notification-service", consumerOptions!.GroupId);
        Assert.Equal("notification-service-retry", consumerOptions.RetryGroupId);
        Assert.Equal("dev.flowchat.notification.email.v1", consumerOptions.Topic);
        Assert.Equal("dev.flowchat.notification.email.v1.retry", consumerOptions.RetryTopic);
        Assert.Equal("dev.flowchat.notification.email.v1.dlq", consumerOptions.DeadLetterTopic);
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

    private static string GetRepositoryPath(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}' starting from '{AppContext.BaseDirectory}'.");
    }
}

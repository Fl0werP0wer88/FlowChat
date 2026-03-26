using System.Collections;
using FlowChat.SocialGraphService.Consumers;
using FlowChat.SocialGraphService.Consumers.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileConsumerConfigurationTests
{
    [Fact]
    public void AddConsumers_RegistersMainAndRetryConsumers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SocialGraphApi:BaseUrl"] = "https://localhost:7194",
                ["SocialGraphApi:ApiKey"] = "worker-key",
                ["Kafka:UserProfileConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileConsumer:GroupId"] = "socialgraph-service",
                ["Kafka:UserProfileConsumer:RetryGroupId"] = "socialgraph-service-retry",
                ["Kafka:UserProfileConsumer:Topic"] = "dev.flowchat.user-profile.user-profile.v1",
                ["Kafka:UserProfileConsumer:RetryTopic"] = "dev.flowchat.user-profile.user-profile.v1.retry",
                ["Kafka:UserProfileConsumer:DeadLetterTopic"] = "dev.flowchat.user-profile.user-profile.v1.dlq",
                ["Kafka:UserProfileConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserProfileConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserProfileConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:UserProfileConsumer:AutoOffsetReset"] = "Earliest"
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
                "dev.flowchat.user-profile.user-profile.v1",
                "dev.flowchat.user-profile.user-profile.v1.retry"
            ],
            configuredTopics);
    }

    [Theory]
    [InlineData("SocialGraphService/src/Workers/FlowChat.SocialGraphService.Consumers/appsettings.json")]
    [InlineData("SocialGraphService/src/Workers/FlowChat.SocialGraphService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(UserProfileConsumerOptions.SectionName)
            .Get<UserProfileConsumerOptions>();

        Assert.NotNull(consumerOptions);
        Assert.Equal("socialgraph-service", consumerOptions!.GroupId);
        Assert.Equal("socialgraph-service-retry", consumerOptions.RetryGroupId);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1", consumerOptions.Topic);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1.retry", consumerOptions.RetryTopic);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1.dlq", consumerOptions.DeadLetterTopic);
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

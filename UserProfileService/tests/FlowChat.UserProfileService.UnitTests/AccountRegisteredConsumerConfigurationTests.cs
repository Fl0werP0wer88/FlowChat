using System.Collections;
using FlowChat.UserProfileService.Consumers;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AccountRegisteredConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersMainAndRetryConsumers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UserProfileApi:BaseUrl"] = "https://localhost:7148",
                ["UserProfileApi:ApiKey"] = "worker-key",
                ["Kafka:AccountRegisteredConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:AccountRegisteredConsumer:GroupId"] = "userprofile-service",
                ["Kafka:AccountRegisteredConsumer:RetryGroupId"] = "userprofile-service-retry",
                ["Kafka:AccountRegisteredConsumer:Topic"] = "dev.flowchat.identity.user.v1",
                ["Kafka:AccountRegisteredConsumer:RetryTopic"] = "dev.flowchat.identity.user.v1.retry",
                ["Kafka:AccountRegisteredConsumer:DeadLetterTopic"] = "dev.flowchat.identity.user.v1.dlq",
                ["Kafka:AccountRegisteredConsumer:MaxRetryCount"] = "5",
                ["Kafka:AccountRegisteredConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:AccountRegisteredConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:AccountRegisteredConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

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
                "dev.flowchat.identity.user.v1",
                "dev.flowchat.identity.user.v1.retry"
            ],
            configuredTopics);
    }

    [Theory]
    [InlineData("UserProfileService/src/Workers/FlowChat.UserProfileService.Consumers/appsettings.json")]
    [InlineData("UserProfileService/src/Workers/FlowChat.UserProfileService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(AccountRegisteredConsumerOptions.SectionName)
            .Get<AccountRegisteredConsumerOptions>();

        Assert.NotNull(consumerOptions);
        Assert.Equal("userprofile-service", consumerOptions!.GroupId);
        Assert.Equal("userprofile-service-retry", consumerOptions.RetryGroupId);
        Assert.Equal("dev.flowchat.identity.user.v1", consumerOptions.Topic);
        Assert.Equal("dev.flowchat.identity.user.v1.retry", consumerOptions.RetryTopic);
        Assert.Equal("dev.flowchat.identity.user.v1.dlq", consumerOptions.DeadLetterTopic);
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

    [Fact]
    public async Task AddConsumers_RegistersUserProfileInternalApiNamedClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UserProfileApi:BaseUrl"] = "https://localhost:7148",
                ["UserProfileApi:ApiKey"] = "worker-key",
                ["Kafka:AccountRegisteredConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:AccountRegisteredConsumer:GroupId"] = "userprofile-service",
                ["Kafka:AccountRegisteredConsumer:RetryGroupId"] = "userprofile-service-retry",
                ["Kafka:AccountRegisteredConsumer:Topic"] = "dev.flowchat.identity.user.v1",
                ["Kafka:AccountRegisteredConsumer:RetryTopic"] = "dev.flowchat.identity.user.v1.retry",
                ["Kafka:AccountRegisteredConsumer:DeadLetterTopic"] = "dev.flowchat.identity.user.v1.dlq",
                ["Kafka:AccountRegisteredConsumer:MaxRetryCount"] = "5",
                ["Kafka:AccountRegisteredConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:AccountRegisteredConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:AccountRegisteredConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var internalApiClient = serviceProvider.GetRequiredService<IUserProfileInternalApiClient>();
        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(UserProfileInternalApiClient.HttpClientName);

        Assert.NotNull(internalApiClient);
        Assert.Equal(new Uri("https://localhost:7148"), httpClient.BaseAddress);
        Assert.Equal("worker-key", httpClient.DefaultRequestHeaders.GetValues(UserProfileInternalApiClient.ApiKeyHeaderName).Single());
    }
}

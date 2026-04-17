using FlowChat.UserProfileService.Consumers;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.UserProfileService.IntegrationTests;

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
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var subscriber = serviceProvider.GetRequiredService<AccountRegisteredSubscriber>();

        consumerCollection.Should().NotBeNull();
        subscriber.Should().NotBeNull();
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
            .GetSection(new AccountRegisteredConsumerSettingsSection().SectionName)
            .Get<AccountRegisteredConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.GroupId.Should().Be("userprofile-service");
        consumerOptions.RetryGroupId.Should().Be("userprofile-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.identity.user.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.identity.user.v1.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.identity.user.v1.dlq");
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

        internalApiClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new Uri("https://localhost:7148"));
        httpClient.DefaultRequestHeaders.GetValues(UserProfileInternalApiClient.ApiKeyHeaderName).Single().Should().Be("worker-key");
    }
}

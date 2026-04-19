using FlowChat.SocialGraphService.Consumers;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Consumers.Configuration;
using FlowChat.SocialGraphService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersConsumerInfrastructure()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var createdSubscriber = scope.ServiceProvider.GetRequiredService<UserProfileCreatedSubscriber>();
        var stateChangedSubscriber = scope.ServiceProvider.GetRequiredService<UserProfileStateChangedSubscriber>();
        var internalApiClient = scope.ServiceProvider.GetRequiredService<ISocialGraphInternalApiClient>();

        consumerCollection.Should().NotBeNull();
        createdSubscriber.Should().NotBeNull();
        stateChangedSubscriber.Should().NotBeNull();
        internalApiClient.Should().NotBeNull();
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
            .GetSection(new UserProfileConsumerSettingsSection().SectionName)
            .Get<UserProfileConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.GroupId.Should().Be("socialgraph-service");
        consumerOptions.RetryGroupId.Should().Be("socialgraph-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.dlq");
    }

    [Fact]
    public async Task AddConsumers_RegistersSocialGraphInternalApiNamedClient()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var internalApiClient = serviceProvider.GetRequiredService<ISocialGraphInternalApiClient>();
        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(SocialGraphInternalApiClient.HttpClientName);

        internalApiClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new Uri("https://localhost:7194"));
        httpClient.DefaultRequestHeaders.GetValues(SocialGraphInternalApiClient.ApiKeyHeaderName).Single()
            .Should().Be("worker-key");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
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

using Confluent.Kafka;
using FlowChat.ChatService.Consumers;
using FlowChat.ChatService.Consumers.Configuration.Settings;
using FlowChat.ChatService.Consumers.Kafka;
using FlowChat.ChatService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.ChatService.UnitTests.Workers.Consumers.Kafka;

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
        var batchSubscriber = scope.ServiceProvider.GetRequiredService<UserProfileProjectionBatchSubscriber>();
        var retrySubscriber = scope.ServiceProvider.GetRequiredService<UserProfileProjectionRetrySubscriber>();
        var internalApiClient = scope.ServiceProvider.GetRequiredService<IChatInternalApiClient>();

        consumerCollection.Should().NotBeNull();
        batchSubscriber.Should().NotBeNull();
        retrySubscriber.Should().NotBeNull();
        internalApiClient.Should().NotBeNull();
    }

    [Fact]
    public async Task AddConsumers_ConfiguresBatchProcessingOnlyForMainConsumer()
    {
        var options = CreateConfiguration()
            .GetSection(new UserProfileConsumerSettingsSection().SectionName)
            .Get<UserProfileConsumerSettingsSection>()!;

        var mainEndpoint = await GetEndpointConfigurationAsync(endpoint => endpoint
            .ConfigureFlowChatMainEndpoint(options)
            .EnableBatchProcessing(
                options.BatchSize,
                TimeSpan.FromMilliseconds(options.BatchMaxWaitTimeMilliseconds)));
        var retryEndpoint = await GetEndpointConfigurationAsync(endpoint =>
            endpoint.ConfigureFlowChatRetryEndpoint(options));

        mainEndpoint.Batch.Should().NotBeNull();
        retryEndpoint.Batch.Should().BeNull();
    }

    [Theory]
    [InlineData("ChatService/src/Workers/FlowChat.ChatService.Consumers/appsettings.json")]
    [InlineData("ChatService/src/Workers/FlowChat.ChatService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(new UserProfileConsumerSettingsSection().SectionName)
            .Get<UserProfileConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.GroupId.Should().Be("chat-service");
        consumerOptions.RetryGroupId.Should().Be("chat-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.chat-service.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.chat-service.dlq");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatApi:BaseUrl"] = "https://localhost:7254",
                ["ChatApi:ApiKey"] = "worker-key",
                ["Kafka:UserProfileConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileConsumer:GroupId"] = "chat-service",
                ["Kafka:UserProfileConsumer:RetryGroupId"] = "chat-service-retry",
                ["Kafka:UserProfileConsumer:Topic"] = "dev.flowchat.user-profile.user-profile.v1",
                ["Kafka:UserProfileConsumer:RetryTopic"] = "dev.flowchat.user-profile.user-profile.v1.chat-service.retry",
                ["Kafka:UserProfileConsumer:DeadLetterTopic"] = "dev.flowchat.user-profile.user-profile.v1.chat-service.dlq",
                ["Kafka:UserProfileConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserProfileConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserProfileConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:UserProfileConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:UserProfileConsumer:BatchSize"] = "100",
                ["Kafka:UserProfileConsumer:BatchMaxWaitTimeMilliseconds"] = "1000"
            })
            .Build();
    }

    private static async Task<KafkaConsumerEndpointConfiguration> GetEndpointConfigurationAsync(
        Func<KafkaConsumerEndpointConfigurationBuilder<object>, KafkaConsumerEndpointConfigurationBuilder<object>> configureEndpoint)
    {
        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var builder = new KafkaConsumerConfigurationBuilder(serviceProvider)
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("test-group")
            .WithAutoOffsetReset(AutoOffsetReset.Earliest)
            .Consume(endpoint => configureEndpoint(endpoint));

        var configuration = builder.Build();

        return configuration.Endpoints.Should().ContainSingle()
            .Which.Should().BeOfType<KafkaConsumerEndpointConfiguration>().Subject;
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

using FlowChat.RealtimeService.Consumers;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Services;
using FlowChat.RealtimeService.Routing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Silverback.Messaging.Broker;
using Testcontainers.Redis;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConsumersConfigurationTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public Task InitializeAsync() => _redisContainer.StartAsync();

    public Task DisposeAsync() => _redisContainer.DisposeAsync().AsTask();

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
        var chatSubscriber = scope.ServiceProvider.GetRequiredService<ChatMessageSentSubscriber>();
        var presenceSubscriber = scope.ServiceProvider.GetRequiredService<UserPresenceChangedSubscriber>();
        var routingTopologyReader = serviceProvider.GetRequiredService<IRealtimeRoutingTopologyReader>();
        var eventRouter = scope.ServiceProvider.GetRequiredService<IRealtimeEventRouter>();
        var internalApiClient = scope.ServiceProvider.GetRequiredService<IRealtimeInternalApiClient>();

        consumerCollection.Should().NotBeNull();
        chatSubscriber.Should().NotBeNull();
        presenceSubscriber.Should().NotBeNull();
        routingTopologyReader.Should().NotBeNull();
        eventRouter.Should().NotBeNull();
        internalApiClient.Should().NotBeNull();
    }

    [Fact]
    public async Task AddConsumers_RegistersRealtimeInternalApiNamedClientWithApiKeyHeader()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var internalApiClient = serviceProvider.GetRequiredService<IRealtimeInternalApiClient>();
        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(RealtimeInternalApiClient.HttpClientName);

        internalApiClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().BeNull();
        httpClient.DefaultRequestHeaders.GetValues(RealtimeInternalApiClient.ApiKeyHeaderName).Single()
            .Should().Be("worker-key");
    }

    [Fact]
    public async Task AddConsumers_RoutingTopologyReader_ReadsInstanceIdsFromRedis()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var userId = Guid.NewGuid();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await database.SetAddAsync(RealtimeRoutingKeys.GetUserInstancesKey("flowchat:test", userId), "instance-a");
        await database.SetAddAsync(RealtimeRoutingKeys.GetUserInstancesKey("flowchat:test", userId), "instance-b");

        var reader = serviceProvider.GetRequiredService<IRealtimeRoutingTopologyReader>();

        var result = await reader.GetInstanceIdsByUserAsync([userId], CancellationToken.None);

        result.Should().ContainKey(userId);
        result[userId].Should().BeEquivalentTo(["instance-a", "instance-b"]);
    }

    private IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RealtimeApi:ApiKey"] = "worker-key",
                ["RealtimeApi:Instances:instance-a"] = "http://localhost:5215",
                ["RealtimeApi:Instances:instance-b"] = "http://localhost:5216",
                ["RealtimeRouting:RedisConnectionString"] = _redisContainer.GetConnectionString(),
                ["RealtimeRouting:KeyPrefix"] = "flowchat:test",
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
                ["Kafka:PresenceStatusChangedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:PresenceStatusChangedConsumer:GroupId"] = "realtime-service",
                ["Kafka:PresenceStatusChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:PresenceStatusChangedConsumer:Topic"] = "dev.flowchat.presence.presence",
                ["Kafka:PresenceStatusChangedConsumer:RetryTopic"] = "dev.flowchat.presence.presence.retry",
                ["Kafka:PresenceStatusChangedConsumer:DeadLetterTopic"] = "dev.flowchat.presence.presence.dlq",
                ["Kafka:PresenceStatusChangedConsumer:MaxRetryCount"] = "5",
                ["Kafka:PresenceStatusChangedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:PresenceStatusChangedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:PresenceStatusChangedConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();
    }
}

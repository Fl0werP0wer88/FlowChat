using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FlowChat.RealtimeService.IntegrationTests;

public sealed class RealtimeConnectionRegistryTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public Task InitializeAsync() => _redisContainer.StartAsync();

    public Task DisposeAsync() => _redisContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task RegisterAsync_WritesConnectionHashUserSetAndRoutingReadModel()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();

        var result = await registry.RegisterAsync(userId, "connection-1", CancellationToken.None);

        var connectionEntries = await database.HashGetAllAsync("flowchat:test:connections:connection-1");
        var userConnections = await database.SetMembersAsync($"flowchat:test:user-connections:{userId:D}");
        var userInstances = await database.SetMembersAsync($"flowchat:test:user-instances:{userId:D}");
        var userInstanceCounts = await database.HashGetAllAsync($"flowchat:test:user-instance-counts:{userId:D}");

        result.ActiveConnectionCount.Should().Be(1);
        connectionEntries.Should().Contain(entry => entry.Name == "userId" && entry.Value == userId.ToString());
        connectionEntries.Should().Contain(entry => entry.Name == "connectionId" && entry.Value == "connection-1");
        connectionEntries.Should().Contain(entry => entry.Name == "instanceId" && entry.Value == "test-instance");
        connectionEntries.Should().Contain(entry => entry.Name == "connectedAtUtc");
        connectionEntries.Should().Contain(entry => entry.Name == "lastSeenUtc");
        userConnections.Should().Contain(value => value == "connection-1");
        userInstances.Should().ContainSingle(value => value == "test-instance");
        userInstanceCounts.Should().ContainSingle(entry => entry.Name == "test-instance" && entry.Value == "1");
    }

    [Fact]
    public async Task UnregisterAsync_WhenConnectionCountRemains_KeepsRoutingInstanceAndDecrementsCount()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();

        await registry.RegisterAsync(userId, "connection-2a", CancellationToken.None);
        await registry.RegisterAsync(userId, "connection-2b", CancellationToken.None);

        var result = await registry.UnregisterAsync("connection-2a", CancellationToken.None);

        result.Should().NotBeNull();
        result!.ActiveConnectionCount.Should().Be(1);
        (await database.SetMembersAsync($"flowchat:test:user-instances:{userId:D}"))
            .Should().ContainSingle(value => value == "test-instance");
        (await database.HashGetAsync($"flowchat:test:user-instance-counts:{userId:D}", "test-instance"))
            .Should().Be("1");
    }

    [Fact]
    public async Task UnregisterAsync_RemovesConnectionHashUserSetAndRoutingReadModel()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();
        var userSetKey = $"flowchat:test:user-connections:{userId:D}";

        await registry.RegisterAsync(userId, "connection-2", CancellationToken.None);
        var result = await registry.UnregisterAsync("connection-2", CancellationToken.None);

        result.Should().NotBeNull();
        result!.ActiveConnectionCount.Should().Be(0);
        (await database.KeyExistsAsync("flowchat:test:connections:connection-2")).Should().BeFalse();
        (await database.SetLengthAsync(userSetKey)).Should().Be(0);
        (await database.KeyExistsAsync($"flowchat:test:user-instances:{userId:D}")).Should().BeFalse();
        (await database.KeyExistsAsync($"flowchat:test:user-instance-counts:{userId:D}")).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_ExtendsTtlForActiveEntriesAndRoutingReadModel()
    {
        using var serviceProvider = BuildServiceProvider(connectionTtl: TimeSpan.FromSeconds(20), refreshInterval: TimeSpan.FromSeconds(5));
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();
        var userSetKey = $"flowchat:test:user-connections:{userId:D}";
        var userInstancesKey = $"flowchat:test:user-instances:{userId:D}";
        var userInstanceCountsKey = $"flowchat:test:user-instance-counts:{userId:D}";

        await registry.RegisterAsync(userId, "connection-3", CancellationToken.None);
        await database.KeyExpireAsync("flowchat:test:connections:connection-3", TimeSpan.FromSeconds(2));
        await database.KeyExpireAsync(userSetKey, TimeSpan.FromSeconds(2));
        await database.KeyExpireAsync(userInstancesKey, TimeSpan.FromSeconds(2));
        await database.KeyExpireAsync(userInstanceCountsKey, TimeSpan.FromSeconds(2));

        await registry.RefreshAsync([new RealtimeConnectionRefreshEntry(userId, "connection-3")], CancellationToken.None);

        var connectionTtl = await database.KeyTimeToLiveAsync("flowchat:test:connections:connection-3");
        var setTtl = await database.KeyTimeToLiveAsync(userSetKey);
        var userInstancesTtl = await database.KeyTimeToLiveAsync(userInstancesKey);
        var userInstanceCountsTtl = await database.KeyTimeToLiveAsync(userInstanceCountsKey);

        connectionTtl.Should().NotBeNull();
        setTtl.Should().NotBeNull();
        userInstancesTtl.Should().NotBeNull();
        userInstanceCountsTtl.Should().NotBeNull();
        connectionTtl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(10));
        setTtl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(10));
        userInstancesTtl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(10));
        userInstanceCountsTtl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task RegisterAsync_WhenUserAlreadyHasConnections_ReturnsCurrentConnectionCount()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var userId = Guid.NewGuid();

        await registry.RegisterAsync(userId, "connection-a", CancellationToken.None);
        var result = await registry.RegisterAsync(userId, "connection-b", CancellationToken.None);

        result.ActiveConnectionCount.Should().Be(2);
    }

    [Fact]
    public async Task RoutingTopologyReader_ReturnsActiveInstanceIdsPerUser()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var topologyReader = serviceProvider.GetRequiredService<IRealtimeRoutingTopologyReader>();
        var userWithConnections = Guid.NewGuid();
        var userWithoutConnections = Guid.NewGuid();

        await registry.RegisterAsync(userWithConnections, "connection-routing-a", CancellationToken.None);
        await registry.RegisterAsync(userWithConnections, "connection-routing-b", CancellationToken.None);

        var result = await topologyReader.GetInstanceIdsByUserAsync(
            [Guid.Empty, userWithConnections, userWithConnections, userWithoutConnections],
            CancellationToken.None);

        result.Should().ContainKey(userWithConnections);
        result[userWithConnections].Should().ContainSingle(instanceId => instanceId == "test-instance");
        result.Should().NotContainKey(userWithoutConnections);
    }

    private ServiceProvider BuildServiceProvider(TimeSpan? connectionTtl = null, TimeSpan? refreshInterval = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["RealtimeConnections:InstanceId"] = "test-instance",
                ["RealtimeConnections:KeyPrefix"] = "flowchat:test",
                ["RealtimeConnections:ConnectionTtl"] = (connectionTtl ?? TimeSpan.FromMinutes(5)).ToString(),
                ["RealtimeConnections:RefreshInterval"] = (refreshInterval ?? TimeSpan.FromMinutes(1)).ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddInfrastructureServices(configuration);

        return services.BuildServiceProvider();
    }
}

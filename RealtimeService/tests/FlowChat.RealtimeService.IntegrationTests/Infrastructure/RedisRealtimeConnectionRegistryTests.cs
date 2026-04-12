using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FlowChat.RealtimeService.IntegrationTests;

public sealed class RedisRealtimeConnectionRegistryTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public Task InitializeAsync() => _redisContainer.StartAsync();

    public Task DisposeAsync() => _redisContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task RegisterAsync_WritesConnectionHashAndUserSet()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();

        await registry.RegisterAsync(userId, "connection-1", CancellationToken.None);

        var connectionEntries = await database.HashGetAllAsync("flowchat:test:connections:connection-1");
        var userConnections = await database.SetMembersAsync($"flowchat:test:user-connections:{userId:D}");

        connectionEntries.Should().Contain(entry => entry.Name == "userId" && entry.Value == userId.ToString());
        connectionEntries.Should().Contain(entry => entry.Name == "connectionId" && entry.Value == "connection-1");
        connectionEntries.Should().Contain(entry => entry.Name == "instanceId" && entry.Value == "test-instance");
        connectionEntries.Should().Contain(entry => entry.Name == "connectedAtUtc");
        connectionEntries.Should().Contain(entry => entry.Name == "lastSeenUtc");
        userConnections.Should().Contain(value => value == "connection-1");
    }

    [Fact]
    public async Task UnregisterAsync_RemovesConnectionHashAndUserSetMembership()
    {
        using var serviceProvider = BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();
        var userSetKey = $"flowchat:test:user-connections:{userId:D}";

        await registry.RegisterAsync(userId, "connection-2", CancellationToken.None);
        await registry.UnregisterAsync("connection-2", CancellationToken.None);

        (await database.KeyExistsAsync("flowchat:test:connections:connection-2")).Should().BeFalse();
        (await database.SetLengthAsync(userSetKey)).Should().Be(0);
    }

    [Fact]
    public async Task RefreshAsync_ExtendsTtlForActiveEntries()
    {
        using var serviceProvider = BuildServiceProvider(connectionTtl: TimeSpan.FromSeconds(20), refreshInterval: TimeSpan.FromSeconds(5));
        var registry = serviceProvider.GetRequiredService<IRealtimeConnectionRegistry>();
        var database = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var userId = Guid.NewGuid();
        var userSetKey = $"flowchat:test:user-connections:{userId:D}";

        await registry.RegisterAsync(userId, "connection-3", CancellationToken.None);
        await database.KeyExpireAsync("flowchat:test:connections:connection-3", TimeSpan.FromSeconds(2));
        await database.KeyExpireAsync(userSetKey, TimeSpan.FromSeconds(2));

        await registry.RefreshAsync(["connection-3"], CancellationToken.None);

        var connectionTtl = await database.KeyTimeToLiveAsync("flowchat:test:connections:connection-3");
        var setTtl = await database.KeyTimeToLiveAsync(userSetKey);

        connectionTtl.Should().NotBeNull();
        setTtl.Should().NotBeNull();
        connectionTtl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(10));
        setTtl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(10));
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

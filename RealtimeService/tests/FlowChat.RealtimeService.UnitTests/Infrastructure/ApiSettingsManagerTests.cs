using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void ApiSettingsManager_ResolvesJwtRuntimeAndInternalSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["ApiUrl"] = "https://localhost:5000",
                ["BlazorUrl"] = "https://localhost:5010",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret",
                ["RealtimeConnections:InstanceId"] = "realtime-instance"
            })
            .Build();

        var settingsManager = new ApiSettingsManager(configuration);

        settingsManager.GetJwtSettings().Key.Should().Be("jwt-key");
        settingsManager.GetApiRuntimeSettings().ApiUrl.Should().Be("https://localhost:5000");
        settingsManager.GetInternalApiSettings().ApiKey.Should().Be("internal-key");
        settingsManager.GetRealtimeConnectionsSettings().RedisConnectionString.Should().Be("localhost:6379,password=secret");
        settingsManager.GetRealtimeConnectionsSettings().InstanceId.Should().Be("realtime-instance");
    }

    [Fact]
    public void ApiSettingsManager_WhenRealtimeConnectionsMissing_UsesDefaults()
    {
        var settingsManager = new ApiSettingsManager(new ConfigurationBuilder().Build());

        var settings = settingsManager.GetRealtimeConnectionsSettings();

        settings.KeyPrefix.Should().Be("flowchat:realtime");
        settings.ConnectionTtl.Should().Be(TimeSpan.FromMinutes(5));
        settings.RefreshInterval.Should().Be(TimeSpan.FromMinutes(1));
    }
}

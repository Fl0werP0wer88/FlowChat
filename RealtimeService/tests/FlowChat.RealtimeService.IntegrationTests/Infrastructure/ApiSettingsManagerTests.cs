using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.IntegrationTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersSettingsManager()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettingsSection:Key"] = "jwt-key",
                ["JwtSettingsSection:Issuer"] = "jwt-issuer",
                ["JwtSettingsSection:Audience"] = "jwt-audience",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret",
                ["RealtimeConnections:InstanceId"] = "realtime-instance"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<IApiSettingsManager>().Should().NotBeNull();
    }
}

using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.IntegrationTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void AddApiInfrastructureServices_RegistersSettingsSections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret",
                ["RealtimeConnections:InstanceId"] = "realtime-instance"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<IOptions<InternalApiSettingsSection>>().Value.ApiKey.Should().Be("internal-key");
    }
}

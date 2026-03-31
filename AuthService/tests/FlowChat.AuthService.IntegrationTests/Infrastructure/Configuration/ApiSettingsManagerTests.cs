using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.IntegrationTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersApiSettingsManager()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var settingsManager = serviceProvider.GetRequiredService<IApiSettingsManager>();

        settingsManager.Should().NotBeNull();
        settingsManager.GetJwtSettings().Key.Should().Be("jwt-key");
        settingsManager.GetInternalApiSettings().ApiKey.Should().Be("internal-key");
    }
}

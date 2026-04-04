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
                ["JwtSettings:EncryptionKey"] = "12345678901234567890123456789012",
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
        settingsManager.GetJwtSettings().EncryptionKey.Should().Be("12345678901234567890123456789012");
        settingsManager.GetInternalApiSettings().ApiKey.Should().Be("internal-key");
    }
}

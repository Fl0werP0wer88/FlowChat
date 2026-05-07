using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.IntegrationTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersSettingsSections()
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

        serviceProvider.GetRequiredService<IOptions<JwtSettingsSection>>().Value.Key.Should().Be("jwt-key");
        serviceProvider.GetRequiredService<IOptions<JwtSettingsSection>>().Value.EncryptionKey.Should().Be("12345678901234567890123456789012");
        serviceProvider.GetRequiredService<IOptions<InternalApiSettingsSection>>().Value.ApiKey.Should().Be("internal-key");
    }
}

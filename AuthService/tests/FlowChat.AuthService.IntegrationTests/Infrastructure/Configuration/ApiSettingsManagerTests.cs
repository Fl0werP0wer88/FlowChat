using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.IntegrationTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersSettingsProvider()
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
        var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();

        settingsProvider.Should().NotBeNull();
        settingsProvider.GetSection<JwtSettingsSection>().Key.Should().Be("jwt-key");
        settingsProvider.GetSection<JwtSettingsSection>().EncryptionKey.Should().Be("12345678901234567890123456789012");
        settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey.Should().Be("internal-key");
    }
}

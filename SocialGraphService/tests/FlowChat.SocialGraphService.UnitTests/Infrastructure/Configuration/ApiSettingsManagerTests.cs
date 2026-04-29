using FlowChat.Shared.API.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void GetSettings_WhenConfigured_ReturnsConfiguredValues()
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

        var settingsProvider = new AppSettingsProvider(configuration);
        var jwtSettings = settingsProvider.GetSection<JwtSettingsSection>();
        var internalApiSettings = settingsProvider.GetSection<InternalApiSettingsSection>();

        jwtSettings.Key.Should().Be("jwt-key");
        jwtSettings.Issuer.Should().Be("jwt-issuer");
        jwtSettings.Audience.Should().Be("jwt-audience");
        internalApiSettings.ApiKey.Should().Be("internal-key");
    }

    [Fact]
    public void GetSettings_WhenConfigurationMissing_ReturnsDefaultValues()
    {
        var settingsProvider = new AppSettingsProvider(new ConfigurationBuilder().Build());

        var jwtSettings = settingsProvider.GetSection<JwtSettingsSection>();
        var internalApiSettings = settingsProvider.GetSection<InternalApiSettingsSection>();

        jwtSettings.Key.Should().BeEmpty();
        jwtSettings.Issuer.Should().BeEmpty();
        jwtSettings.Audience.Should().BeEmpty();
        internalApiSettings.ApiKey.Should().BeEmpty();
    }
}

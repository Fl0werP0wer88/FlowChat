using FlowChat.SocialGraphService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void GetSettings_WhenConfigured_ReturnsConfiguredValuesAndRootUrlsOverrideSectionValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["FlowChat:ApiUrl"] = "https://section-api.flowchat.local",
                ["FlowChat:BlazorUrl"] = "https://section-ui.flowchat.local",
                ["ApiUrl"] = "https://root-api.flowchat.local",
                ["BlazorUrl"] = "https://root-ui.flowchat.local"
            })
            .Build();

        var settingsManager = new ApiSettingsManager(configuration);
        var jwtSettings = settingsManager.GetJwtSettingsSection();
        var internalApiSettings = settingsManager.GetInternalApiSettingsSection();
        var apiRuntimeSettings = settingsManager.GetApiRuntimeSettingsSection();

        jwtSettings.Key.Should().Be("jwt-key");
        jwtSettings.Issuer.Should().Be("jwt-issuer");
        jwtSettings.Audience.Should().Be("jwt-audience");
        internalApiSettings.ApiKey.Should().Be("internal-key");
        apiRuntimeSettings.ApiUrl.Should().Be("https://root-api.flowchat.local");
        apiRuntimeSettings.BlazorUrl.Should().Be("https://root-ui.flowchat.local");
    }

    [Fact]
    public void GetSettings_WhenConfigurationMissing_ReturnsDefaultValues()
    {
        var settingsManager = new ApiSettingsManager(new ConfigurationBuilder().Build());

        var jwtSettings = settingsManager.GetJwtSettingsSection();
        var internalApiSettings = settingsManager.GetInternalApiSettingsSection();
        var apiRuntimeSettings = settingsManager.GetApiRuntimeSettingsSection();

        jwtSettings.Key.Should().BeEmpty();
        jwtSettings.Issuer.Should().BeEmpty();
        jwtSettings.Audience.Should().BeEmpty();
        internalApiSettings.ApiKey.Should().BeEmpty();
        apiRuntimeSettings.ApiUrl.Should().Be("https://localhost:5000");
        apiRuntimeSettings.BlazorUrl.Should().Be("https://localhost:5010");
    }
}

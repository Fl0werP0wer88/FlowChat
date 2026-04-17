using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void ApiSettingsManager_ResolvesJwtRuntimeAndInternalApiSettingsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettingsSection:Key"] = "jwt-key",
                ["JwtSettingsSection:EncryptionKey"] = "12345678901234567890123456789012",
                ["JwtSettingsSection:Issuer"] = "jwt-issuer",
                ["JwtSettingsSection:Audience"] = "jwt-audience",
                ["JwtSettingsSection:ExpiresMinutes"] = "90",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ApiUrl"] = "https://localhost:5000",
                ["BlazorUrl"] = "https://localhost:5010"
            })
            .Build();

        var settingsManager = new ApiSettingsManager(configuration);
        var jwtSettings = settingsManager.GetJwtSettingsSection();
        var internalApiSettings = settingsManager.GetInternalApiSettingsSection();
        var apiRuntimeSettings = settingsManager.GetApiRuntimeSettingsSection();

        jwtSettings.Key.Should().Be("jwt-key");
        jwtSettings.EncryptionKey.Should().Be("12345678901234567890123456789012");
        jwtSettings.Issuer.Should().Be("jwt-issuer");
        jwtSettings.Audience.Should().Be("jwt-audience");
        jwtSettings.ExpiresMinutes.Should().Be(90);
        internalApiSettings.ApiKey.Should().Be("internal-key");
        apiRuntimeSettings.ApiUrl.Should().Be("https://localhost:5000");
        apiRuntimeSettings.BlazorUrl.Should().Be("https://localhost:5010");
    }

}

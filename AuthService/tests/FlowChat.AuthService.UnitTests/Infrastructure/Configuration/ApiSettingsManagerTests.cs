using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void ApiSettingsManager_ResolvesJwtRuntimeAndInternalApiSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["JwtSettings:ExpiresMinutes"] = "90",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ApiUrl"] = "https://localhost:5000",
                ["BlazorUrl"] = "https://localhost:5010"
            })
            .Build();

        var settingsManager = new ApiSettingsManager(configuration);
        var jwtSettings = settingsManager.GetJwtSettings();
        var internalApiSettings = settingsManager.GetInternalApiSettings();
        var apiRuntimeSettings = settingsManager.GetApiRuntimeSettings();

        jwtSettings.Key.Should().Be("jwt-key");
        jwtSettings.Issuer.Should().Be("jwt-issuer");
        jwtSettings.Audience.Should().Be("jwt-audience");
        jwtSettings.ExpiresMinutes.Should().Be(90);
        internalApiSettings.ApiKey.Should().Be("internal-key");
        apiRuntimeSettings.ApiUrl.Should().Be("https://localhost:5000");
        apiRuntimeSettings.BlazorUrl.Should().Be("https://localhost:5010");
    }

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

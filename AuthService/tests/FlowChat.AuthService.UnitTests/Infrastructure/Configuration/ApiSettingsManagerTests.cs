using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
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

        Assert.Equal("jwt-key", jwtSettings.Key);
        Assert.Equal("jwt-issuer", jwtSettings.Issuer);
        Assert.Equal("jwt-audience", jwtSettings.Audience);
        Assert.Equal(90, jwtSettings.ExpiresMinutes);
        Assert.Equal("internal-key", internalApiSettings.ApiKey);
        Assert.Equal("https://localhost:5000", apiRuntimeSettings.ApiUrl);
        Assert.Equal("https://localhost:5010", apiRuntimeSettings.BlazorUrl);
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

        Assert.NotNull(settingsManager);
        Assert.Equal("jwt-key", settingsManager.GetJwtSettings().Key);
        Assert.Equal("internal-key", settingsManager.GetInternalApiSettings().ApiKey);
    }
}

using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void ApiSettingsManager_ResolvesJwtConfirmationAndRuntimeSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["JwtSettings:ExpiresMinutes"] = "90",
                ["ConfirmationLinks:EmailConfirmationBaseUrl"] = "https://localhost:7236/api/users/confirm-email",
                ["ApiUrl"] = "https://localhost:5000",
                ["BlazorUrl"] = "https://localhost:5010",
                ["FlowChat:DropDatabaseOnStartup"] = "true",
                ["ConnectionStrings:AuthDb"] =
                    "Host=localhost;Port=5432;Database=flowchat_auth_db;Username=flowchat_app;Password=flowchat_app_pw;"
            })
            .Build();

        var settingsManager = new ApiSettingsManager(configuration);
        var jwtSettings = settingsManager.GetJwtSettings();
        var confirmationLinksSettings = settingsManager.GetConfirmationLinksSettings();
        var apiRuntimeSettings = settingsManager.GetApiRuntimeSettings();

        Assert.Equal("jwt-key", jwtSettings.Key);
        Assert.Equal("jwt-issuer", jwtSettings.Issuer);
        Assert.Equal("jwt-audience", jwtSettings.Audience);
        Assert.Equal(90, jwtSettings.ExpiresMinutes);
        Assert.Equal(
            "https://localhost:7236/api/users/confirm-email",
            confirmationLinksSettings.EmailConfirmationBaseUrl);
        Assert.Equal("https://localhost:5000", apiRuntimeSettings.ApiUrl);
        Assert.Equal("https://localhost:5010", apiRuntimeSettings.BlazorUrl);
        Assert.True(apiRuntimeSettings.DropDatabaseOnStartup);
        Assert.Contains("Host=localhost", apiRuntimeSettings.AuthDbConnectionString, StringComparison.Ordinal);
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
                ["ConfirmationLinks:EmailConfirmationBaseUrl"] = "https://localhost:7236/api/users/confirm-email"
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
        Assert.Equal(
            "https://localhost:7236/api/users/confirm-email",
            settingsManager.GetConfirmationLinksSettings().EmailConfirmationBaseUrl);
    }
}

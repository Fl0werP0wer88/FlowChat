using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void ApiSettingsManager_ResolvesJwtRuntimeAndInternalSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "jwt-key",
                ["JwtSettings:Issuer"] = "jwt-issuer",
                ["JwtSettings:Audience"] = "jwt-audience",
                ["ApiUrl"] = "https://localhost:5000",
                ["BlazorUrl"] = "https://localhost:5010",
                ["FlowChat:InternalApi:ApiKey"] = "internal-key"
            })
            .Build();

        var settingsManager = new ApiSettingsManager(configuration);

        Assert.Equal("jwt-key", settingsManager.GetJwtSettings().Key);
        Assert.Equal("https://localhost:5000", settingsManager.GetApiRuntimeSettings().ApiUrl);
        Assert.Equal("internal-key", settingsManager.GetInternalApiSettings().ApiKey);
    }

    [Fact]
    public void AddInfrastructureServices_RegistersSettingsManager()
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

        Assert.NotNull(serviceProvider.GetRequiredService<IApiSettingsManager>());
    }
}

using FlowChat.GatewayService.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class GatewayRouteConfigurationTests
{
    [Fact]
    public void GatewayAppSettings_ExposeRealtimeRouteAndCluster()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(RepositoryPathHelper.GetRepositoryPath("GatewayService/src/FlowChat.GatewayService.API/appsettings.json"))
            .Build();
        var settingsManager = new ApiSettingsManager(configuration);

        var route = settingsManager
            .GetReverseProxyRouteSettings()
            .Single(settings => settings.RouteId == "realtime-catch-all");

        Assert.Equal("/realtime/{**catch-all}", route.RawPath);
        Assert.Equal("realtime-service", route.ClusterId);
        Assert.Equal("gateway-authenticated", route.AuthorizationPolicy);
        Assert.Equal("http://localhost:5215/", configuration["ReverseProxy:Clusters:realtime-service:Destinations:d1:Address"]);
    }
}

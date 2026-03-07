using Microsoft.Extensions.Configuration;

namespace FlowChat.GatewayService.Api.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private static readonly string[] DefaultMethods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"];
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtSettings GetJwtSettings() =>
        _configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

    public ApiRuntimeSettings GetApiRuntimeSettings()
    {
        return new ApiRuntimeSettings
        {
            ApiUrl = _configuration["ApiUrl"] ?? "https://localhost:5000",
            BlazorUrl = _configuration["BlazorUrl"] ?? "https://localhost:5010"
        };
    }

    public IReadOnlyList<ReverseProxyRouteSettings> GetReverseProxyRouteSettings()
    {
        return _configuration.GetSection("ReverseProxy:Routes")
            .GetChildren()
            .Select(route => new ReverseProxyRouteSettings
            {
                RouteId = route.Key,
                RawPath = route.GetValue<string>("Match:Path") ?? string.Empty,
                ClusterId = route.GetValue<string>("ClusterId") ?? "unknown-cluster",
                AuthorizationPolicy = route.GetValue<string>("AuthorizationPolicy"),
                Methods = route.GetSection("Match:Methods").Get<string[]>() ?? DefaultMethods
            })
            .ToArray();
    }
}

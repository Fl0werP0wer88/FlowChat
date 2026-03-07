namespace FlowChat.GatewayService.Api.Configuration;

public interface IApiSettingsManager
{
    JwtSettings GetJwtSettings();

    ApiRuntimeSettings GetApiRuntimeSettings();

    IReadOnlyList<ReverseProxyRouteSettings> GetReverseProxyRouteSettings();
}

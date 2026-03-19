namespace FlowChat.GatewayService.Api.Models;

public sealed class GatewayRouteResponse
{
    public string Name { get; init; } = string.Empty;

    public string PublicPath { get; init; } = string.Empty;

    public string ClusterId { get; init; } = string.Empty;

    public bool RequiresAuthentication { get; init; }
}

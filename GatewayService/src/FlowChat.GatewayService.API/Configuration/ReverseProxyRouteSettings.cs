namespace FlowChat.GatewayService.Api.Configuration;

public sealed class ReverseProxyRouteSettings
{
    public string RouteId { get; init; } = string.Empty;

    public string RawPath { get; init; } = string.Empty;

    public string ClusterId { get; init; } = "unknown-cluster";

    public string? AuthorizationPolicy { get; init; }

    public string[] Methods { get; init; } = [];
}

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenTelemetry.Instrumentation.AspNetCore;
using Yarp.ReverseProxy.Model;

namespace FlowChat.GatewayService.Api.Observability;

internal static class GatewayTraceEnrichment
{
    private const string CatchAllMarker = "{**catch-all}";
    private const string RequestPathTag = "flowchat.http.request_path";
    private const string GatewayRouteIdTag = "flowchat.gateway.route_id";
    private const string GatewayClusterIdTag = "flowchat.gateway.cluster_id";
    private const string GatewayRoutePatternTag = "flowchat.gateway.route_pattern";

    public static void Configure(AspNetCoreTraceInstrumentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.EnrichWithHttpResponse = static (activity, response) =>
        {
            if (activity is null || response is null)
            {
                return;
            }

            var httpContext = response.HttpContext;
            var requestPath = NormalizeRequestPath(httpContext.Request.Path);
            var routePattern = ResolveRoutePattern(httpContext);
            var proxyFeature = GetReverseProxyFeatureOrNull(httpContext);

            activity.SetTag(RequestPathTag, requestPath);

            if (!string.IsNullOrWhiteSpace(routePattern))
            {
                activity.SetTag(GatewayRoutePatternTag, routePattern);
            }

            var routeId = proxyFeature?.Route?.Config?.RouteId;
            if (!string.IsNullOrWhiteSpace(routeId))
            {
                activity.SetTag(GatewayRouteIdTag, routeId);
            }

            var clusterId = proxyFeature?.Cluster?.Config?.ClusterId;
            if (!string.IsNullOrWhiteSpace(clusterId))
            {
                activity.SetTag(GatewayClusterIdTag, clusterId);
            }

            if (routePattern?.Contains(CatchAllMarker, StringComparison.Ordinal) == true)
            {
                activity.DisplayName = $"{httpContext.Request.Method} {requestPath}";
            }
        };
    }

    private static string NormalizeRequestPath(PathString path) =>
        string.IsNullOrWhiteSpace(path.Value) ? "/" : path.Value;

    private static string? ResolveRoutePattern(HttpContext httpContext)
    {
        var routeEndpoint = httpContext.GetEndpoint() as RouteEndpoint;
        if (!string.IsNullOrWhiteSpace(routeEndpoint?.RoutePattern.RawText))
        {
            return routeEndpoint.RoutePattern.RawText;
        }

        return GetReverseProxyFeatureOrNull(httpContext)?.Route?.Config?.Match?.Path;
    }

    private static IReverseProxyFeature? GetReverseProxyFeatureOrNull(HttpContext httpContext) =>
        httpContext.Features.Get<IReverseProxyFeature>();
}

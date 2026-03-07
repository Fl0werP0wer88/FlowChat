using System.Text.RegularExpressions;
using System.Net.Http;
using FlowChat.GatewayService.Api.Configuration;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FlowChat.GatewayService.Api.OpenApi;

public sealed class ReverseProxyRoutesDocumentFilter : IDocumentFilter
{
    private static readonly Regex CatchAllPattern = new(@"\{\*\*([^}]+)\}", RegexOptions.Compiled);

    private readonly IApiSettingsManager _apiSettingsManager;

    public ReverseProxyRoutesDocumentFilter(IApiSettingsManager apiSettingsManager)
    {
        _apiSettingsManager = apiSettingsManager;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var routes = _apiSettingsManager.GetReverseProxyRouteSettings();

        foreach (var route in routes)
        {
            var rawPath = route.RawPath;
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var path = NormalizePath(rawPath);
            if (!swaggerDoc.Paths.TryGetValue(path, out var existingPathItem)
                || existingPathItem is not OpenApiPathItem pathItem)
            {
                pathItem = new OpenApiPathItem();
                swaggerDoc.Paths[path] = pathItem;
            }

            foreach (var method in route.Methods)
            {
                var httpMethod = ToHttpMethod(method);
                if (httpMethod is null)
                {
                    continue;
                }

                if (pathItem.Operations is not null && pathItem.Operations.ContainsKey(httpMethod))
                {
                    continue;
                }

                pathItem.AddOperation(httpMethod, BuildOperation(route, method, rawPath));
            }
        }
    }

    private static OpenApiOperation BuildOperation(
        ReverseProxyRouteSettings route,
        string method,
        string rawPath)
    {
        var routeId = route.RouteId;
        var clusterId = route.ClusterId;
        var authPolicy = route.AuthorizationPolicy;
        var authInfo = string.IsNullOrWhiteSpace(authPolicy)
            ? "Authentication: not required."
            : $"Authentication policy: {authPolicy}.";

        return new OpenApiOperation
        {
            Summary = $"Proxy route: {method.ToUpperInvariant()} {rawPath}",
            Description = $"Forwarded by gateway route '{routeId}' to cluster '{clusterId}'. {authInfo}",
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse { Description = "Success" },
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                ["403"] = new OpenApiResponse { Description = "Forbidden" },
                ["404"] = new OpenApiResponse { Description = "Not Found" },
                ["500"] = new OpenApiResponse { Description = "Internal Server Error" }
            }
        };
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.StartsWith("/", StringComparison.Ordinal)
            ? path
            : "/" + path;

        return CatchAllPattern.Replace(normalized, "{$1}");
    }

    private static HttpMethod? ToHttpMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            "HEAD" => HttpMethod.Head,
            "OPTIONS" => HttpMethod.Options,
            "TRACE" => HttpMethod.Trace,
            _ => null
        };
    }
}

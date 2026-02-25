using System.Text.RegularExpressions;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FlowChat.GatewayService.Api.OpenApi;

public sealed class ReverseProxyRoutesDocumentFilter : IDocumentFilter
{
    private static readonly Regex CatchAllPattern = new(@"\{\*\*([^}]+)\}", RegexOptions.Compiled);

    private readonly IConfiguration _configuration;

    public ReverseProxyRoutesDocumentFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var routes = _configuration.GetSection("ReverseProxy:Routes").GetChildren();

        foreach (var route in routes)
        {
            var rawPath = route.GetValue<string>("Match:Path");
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

            var methods = route.GetSection("Match:Methods").Get<string[]>();
            if (methods is null || methods.Length == 0)
            {
                methods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"];
            }

            foreach (var method in methods)
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

    private static OpenApiOperation BuildOperation(IConfigurationSection route, string method, string rawPath)
    {
        var routeId = route.Key;
        var clusterId = route.GetValue<string>("ClusterId") ?? "unknown-cluster";
        var authPolicy = route.GetValue<string>("AuthorizationPolicy");
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

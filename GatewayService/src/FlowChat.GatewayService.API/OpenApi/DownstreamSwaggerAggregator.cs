using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FlowChat.GatewayService.Api.OpenApi;

public sealed class DownstreamSwaggerAggregator
{
    private static readonly HashSet<string> OperationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get",
        "post",
        "put",
        "delete",
        "patch",
        "head",
        "options",
        "trace"
    };

    private static readonly HashSet<string> PrefixedComponentSections = new(StringComparer.Ordinal)
    {
        "schemas",
        "responses",
        "parameters",
        "examples",
        "requestBodies",
        "headers",
        "links",
        "callbacks",
        "pathItems"
    };

    private readonly HttpClient _httpClient;
    private readonly IOptions<SwaggerAggregationOptions> _options;
    private readonly ILogger<DownstreamSwaggerAggregator> _logger;

    public DownstreamSwaggerAggregator(
        HttpClient httpClient,
        IOptions<SwaggerAggregationOptions> options,
        ILogger<DownstreamSwaggerAggregator> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<string> BuildDocumentAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var targetDocument = CreateBaseDocument(context);
        var targetPaths = targetDocument["paths"]!.AsObject();
        var targetComponents = targetDocument["components"]!.AsObject();
        var targetSecurity = targetDocument["security"]!.AsArray();

        foreach (var source in _options.Value.Sources)
        {
            if (string.IsNullOrWhiteSpace(source.SwaggerUrl))
            {
                continue;
            }

            var sourceDocument = await LoadSourceDocumentAsync(source.SwaggerUrl, cancellationToken);
            if (sourceDocument is null)
            {
                continue;
            }

            var sourceKey = BuildSourceKey(source.Name, source.GatewayPrefix);
            var gatewayPrefix = NormalizeGatewayPrefix(source.GatewayPrefix, sourceKey);

            MergePaths(sourceDocument, targetPaths, sourceKey, gatewayPrefix);
            MergeComponents(sourceDocument, targetComponents, sourceKey);
            MergeSecurity(sourceDocument, targetSecurity);
        }

        return targetDocument.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<JsonObject?> LoadSourceDocumentAsync(string swaggerUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(swaggerUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Skipping Swagger source {SwaggerUrl}. HTTP status code: {StatusCode}.",
                    swaggerUrl,
                    (int)response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (JsonNode.Parse(content) is not JsonObject sourceDocument)
            {
                _logger.LogWarning("Skipping Swagger source {SwaggerUrl}. Response is not a valid JSON object.", swaggerUrl);
                return null;
            }

            return sourceDocument;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skipping Swagger source {SwaggerUrl}. Failed to download or parse.", swaggerUrl);
            return null;
        }
    }

    private static JsonObject CreateBaseDocument(HttpContext context)
    {
        var serverUrl = $"{context.Request.Scheme}://{context.Request.Host}";

        return new JsonObject
        {
            ["openapi"] = "3.0.1",
            ["info"] = new JsonObject
            {
                ["title"] = "FlowChat Gateway Aggregated API",
                ["version"] = "v1",
                ["description"] = "Aggregated OpenAPI document generated from downstream services."
            },
            ["servers"] = new JsonArray
            {
                new JsonObject
                {
                    ["url"] = serverUrl
                }
            },
            ["paths"] = new JsonObject(),
            ["components"] = new JsonObject(),
            ["security"] = new JsonArray()
        };
    }

    private static void MergePaths(
        JsonObject sourceDocument,
        JsonObject targetPaths,
        string sourceKey,
        string gatewayPrefix)
    {
        if (sourceDocument["paths"] is not JsonObject sourcePaths)
        {
            return;
        }

        foreach (var sourcePath in sourcePaths)
        {
            if (sourcePath.Value is null)
            {
                continue;
            }

            var targetPath = CombinePath(gatewayPrefix, sourcePath.Key);
            if (CloneAndRewriteRefs(sourcePath.Value, sourceKey) is not JsonObject pathItem)
            {
                continue;
            }

            DecorateOperations(pathItem, sourceKey);

            if (targetPaths[targetPath] is JsonObject existingPathItem)
            {
                MergePathItems(existingPathItem, pathItem);
                continue;
            }

            targetPaths[targetPath] = pathItem;
        }
    }

    private static void MergePathItems(JsonObject existingPathItem, JsonObject newPathItem)
    {
        foreach (var property in newPathItem)
        {
            existingPathItem[property.Key] = property.Value;
        }
    }

    private static void DecorateOperations(JsonObject pathItem, string sourceKey)
    {
        foreach (var operationMethod in OperationMethods)
        {
            if (pathItem[operationMethod] is not JsonObject operation)
            {
                continue;
            }

            EnsureOperationId(operation, sourceKey);
        }
    }

    private static void EnsureOperationId(JsonObject operation, string sourceKey)
    {
        if (operation["operationId"] is JsonValue operationIdValue
            && operationIdValue.TryGetValue<string>(out var operationId)
            && !string.IsNullOrWhiteSpace(operationId))
        {
            operation["operationId"] = $"{sourceKey}_{operationId}";
        }
    }

    private static void MergeComponents(JsonObject sourceDocument, JsonObject targetComponents, string sourceKey)
    {
        if (sourceDocument["components"] is not JsonObject sourceComponents)
        {
            return;
        }

        foreach (var sourceComponentSection in sourceComponents)
        {
            if (sourceComponentSection.Value is not JsonObject sourceComponentValues)
            {
                continue;
            }

            if (targetComponents[sourceComponentSection.Key] is not JsonObject targetComponentValues)
            {
                targetComponentValues = new JsonObject();
                targetComponents[sourceComponentSection.Key] = targetComponentValues;
            }

            foreach (var sourceComponent in sourceComponentValues)
            {
                var targetKey = ShouldPrefixComponentSection(sourceComponentSection.Key)
                    ? $"{sourceKey}__{sourceComponent.Key}"
                    : sourceComponent.Key;

                if (targetComponentValues[targetKey] is not null)
                {
                    continue;
                }

                targetComponentValues[targetKey] = CloneAndRewriteRefs(sourceComponent.Value, sourceKey);
            }
        }
    }

    private static void MergeSecurity(JsonObject sourceDocument, JsonArray targetSecurity)
    {
        if (sourceDocument["security"] is not JsonArray sourceSecurity)
        {
            return;
        }

        foreach (var securityRequirement in sourceSecurity)
        {
            if (securityRequirement is null)
            {
                continue;
            }

            targetSecurity.Add(securityRequirement.DeepClone());
        }
    }

    private static JsonNode? CloneAndRewriteRefs(JsonNode? sourceNode, string sourceKey)
    {
        if (sourceNode is null)
        {
            return null;
        }

        var clonedNode = sourceNode.DeepClone();
        RewriteRefs(clonedNode, sourceKey);
        return clonedNode;
    }

    private static void RewriteRefs(JsonNode node, string sourceKey)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (string.Equals(property.Key, "$ref", StringComparison.OrdinalIgnoreCase)
                        && property.Value is JsonValue refValue
                        && refValue.TryGetValue<string>(out var refString))
                    {
                        jsonObject[property.Key] = RewriteRef(refString, sourceKey);
                        continue;
                    }

                    if (property.Value is not null)
                    {
                        RewriteRefs(property.Value, sourceKey);
                    }
                }
                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    if (item is not null)
                    {
                        RewriteRefs(item, sourceKey);
                    }
                }
                break;
        }
    }

    private static string RewriteRef(string reference, string sourceKey)
    {
        const string componentsPrefix = "#/components/";
        if (!reference.StartsWith(componentsPrefix, StringComparison.Ordinal))
        {
            return reference;
        }

        var remainder = reference[componentsPrefix.Length..];
        var separatorIndex = remainder.IndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= remainder.Length - 1)
        {
            return reference;
        }

        var sectionName = remainder[..separatorIndex];
        var key = remainder[(separatorIndex + 1)..];
        if (!ShouldPrefixComponentSection(sectionName))
        {
            return reference;
        }

        return $"{componentsPrefix}{sectionName}/{sourceKey}__{key}";
    }

    private static bool ShouldPrefixComponentSection(string componentSectionName)
        => PrefixedComponentSections.Contains(componentSectionName);

    private static string BuildSourceKey(string? sourceName, string? gatewayPrefix)
    {
        var candidate = string.IsNullOrWhiteSpace(sourceName) ? gatewayPrefix ?? string.Empty : sourceName.Trim();
        candidate = Regex.Replace(candidate, "[^A-Za-z0-9]", string.Empty).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(candidate) ? "service" : candidate;
    }

    private static string NormalizeGatewayPrefix(string? gatewayPrefix, string sourceKey)
    {
        var prefix = string.IsNullOrWhiteSpace(gatewayPrefix)
            ? "/" + sourceKey
            : gatewayPrefix.Trim();

        if (!prefix.StartsWith("/", StringComparison.Ordinal))
        {
            prefix = "/" + prefix;
        }

        prefix = prefix.TrimEnd('/');
        return string.IsNullOrWhiteSpace(prefix) ? "/" : prefix;
    }

    private static string CombinePath(string gatewayPrefix, string sourcePath)
    {
        var normalizedSourcePath = sourcePath.StartsWith("/", StringComparison.Ordinal)
            ? sourcePath
            : "/" + sourcePath;

        if (gatewayPrefix == "/")
        {
            return normalizedSourcePath;
        }

        if (normalizedSourcePath == "/")
        {
            return gatewayPrefix;
        }

        return gatewayPrefix + normalizedSourcePath;
    }
}

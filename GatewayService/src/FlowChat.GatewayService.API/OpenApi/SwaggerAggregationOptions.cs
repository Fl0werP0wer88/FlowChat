namespace FlowChat.GatewayService.Api.OpenApi;

public sealed class SwaggerAggregationOptions
{
    public const string SectionName = "SwaggerAggregation";

    public List<SwaggerAggregationSourceOptions> Sources { get; init; } = [];
}

public sealed class SwaggerAggregationSourceOptions
{
    public string Name { get; init; } = string.Empty;

    public string SwaggerUrl { get; init; } = string.Empty;

    public string GatewayPrefix { get; init; } = string.Empty;
}

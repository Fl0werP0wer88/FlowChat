using FlowChat.GatewayService.Api.OpenApi;
using Microsoft.OpenApi;

namespace FlowChat.GatewayService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<SwaggerAggregationOptions>(
            builder.Configuration.GetSection(SwaggerAggregationOptions.SectionName));
        builder.Services.AddHttpClient<DownstreamSwaggerAggregator>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers();

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins(
                        [
                            builder.Configuration["ApiUrl"] ?? "https://localhost:5000",
                            builder.Configuration["BlazorUrl"] ?? "https://localhost:5010"
                        ])
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowCredentials()));

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FlowChat Gateway API",
                Version = "v1",
                Description = "Gateway routes and downstream service OpenAPI references."
            });
            options.DocumentFilter<ReverseProxyRoutesDocumentFilter>();
        });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCors("open");

        if (app.Environment.IsDevelopment())
        {
            app.MapGet(
                "/openapi/aggregated.json",
                async (DownstreamSwaggerAggregator aggregator, HttpContext context, CancellationToken cancellationToken) =>
                {
                    var document = await aggregator.BuildDocumentAsync(context, cancellationToken);
                    return Results.Text(document, "application/json");
                })
                .ExcludeFromDescription();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/aggregated.json", "GatewayService Aggregated API");
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "GatewayService Routes (Raw)");
            });
        }

        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "GatewayService" }));
        app.MapControllers();
        app.MapReverseProxy();

        return app;
    }
}

using FlowChat.API.Abstractions;
using FlowChat.GatewayService.Api.Configuration;
using FlowChat.GatewayService.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace FlowChat.GatewayService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var apiSettingsManager = new ApiSettingsManager(builder.Configuration);
        var apiRuntimeSettings = apiSettingsManager.GetApiRuntimeSettings();
        var jwtSettings = apiSettingsManager.GetJwtSettings();
        var jwtKey = jwtSettings.Key;
        var jwtIssuer = jwtSettings.Issuer;
        var jwtAudience = jwtSettings.Audience;

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Key.");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Audience.");
        }

        builder.Services.TryAddSingleton<IApiSettingsManager>(apiSettingsManager);
        builder.Services.Configure<SwaggerAggregationOptions>(
            builder.Configuration.GetSection(SwaggerAggregationOptions.SectionName));
        builder.Services.AddHttpClient<DownstreamSwaggerAggregator>();
        builder.AddFlowChatOpenTelemetry();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers();

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins(
                        [
                            apiRuntimeSettings.ApiUrl,
                            apiRuntimeSettings.BlazorUrl
                        ])
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowCredentials()));

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("gateway-authenticated", policy => policy.RequireAuthenticatedUser());
        });

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
        app.UseFlowChatGlobalExceptionHandling();

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
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "GatewayService" }));
        app.MapControllers();
        app.MapReverseProxy();

        return app;
    }
}

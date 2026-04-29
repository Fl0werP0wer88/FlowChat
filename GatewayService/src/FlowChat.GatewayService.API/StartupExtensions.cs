using FlowChat.GatewayService.Api.Configuration.Settings;
using FlowChat.GatewayService.Api.Observability;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.API;
using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace FlowChat.GatewayService.Api;

public static class StartupExtensions
{
    private const string ClientCorsPolicyName = "gateway-client";
    private const string AuthenticatedUserPolicyName = "GatewayAuthenticated";

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddFlowChatOpenTelemetry();
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(GatewayTraceEnrichment.Configure);

        var settingsProvider = new AppSettingsProvider(builder.Configuration);
        var clientSettings = settingsProvider.GetSection<GatewayClientSettingsSection>();
        var servicesSettings = settingsProvider.GetSection<GatewayServicesSettingsSection>();

        builder.Services.Configure<GatewayCatalogSettingsSection>(
            builder.Configuration.GetSection(new GatewayCatalogSettingsSection().SectionName));

        builder.Services.AddFlowChatJwtAuthentication(
            builder.Configuration,
            configureJwtBearer: options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            },
            configureAuthorization: options =>
            {
                options.AddPolicy(
                    AuthenticatedUserPolicyName,
                    policy => policy.RequireAuthenticatedUser());

                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                ClientCorsPolicyName,
                policy =>
                {
                    if (clientSettings.AllowedOrigins.Count > 0)
                    {
                        policy.WithOrigins(clientSettings.AllowedOrigins.ToArray());
                    }

                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddFlowChatSwaggerWithBearer(options =>
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "FlowChat GatewayService API",
                    Version = "v1",
                    Description = "Direct endpoints exposed by the FlowChat gateway."
                }));

        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<BearerTokenForwardingHandler>();

        builder.Services
            .AddHttpClient<ISocialGraphServiceClient, SocialGraphServiceClient>(client =>
                client.BaseAddress = new Uri(servicesSettings.SocialGraphServiceBaseUrl))
            .AddHttpMessageHandler<BearerTokenForwardingHandler>();

        builder.Services
            .AddHttpClient<IChatServiceClient, ChatServiceClient>(client =>
                client.BaseAddress = new Uri(servicesSettings.ChatServiceBaseUrl))
            .AddHttpMessageHandler<BearerTokenForwardingHandler>();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCors(ClientCorsPolicyName);
        app.UseFlowChatGlobalExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "FlowChat GatewayService API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseWebSockets();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapReverseProxy();

        return app;
    }
}

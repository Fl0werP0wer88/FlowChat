using System.Text.Json.Serialization;
using FlowChat.GatewayService.Api.Configuration.Settings;
using FlowChat.GatewayService.Api.Observability;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.API;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Http;
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

        builder.Services.AddSettingsSections(builder.Configuration, typeof(StartupExtensions).Assembly);

        var clientSettings = builder.Configuration.GetSection(new GatewayClientSettingsSection().SectionName)
            .Get<GatewayClientSettingsSection>() ?? new GatewayClientSettingsSection();
        var servicesSettings = builder.Configuration.GetSection(new GatewayServicesSettingsSection().SectionName)
            .Get<GatewayServicesSettingsSection>() ?? new GatewayServicesSettingsSection();

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

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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
            .AddFlowChatHttpClient<ISocialGraphServiceClient, SocialGraphServiceClient>((_, client) =>
                client.BaseAddress = new Uri(servicesSettings.SocialGraphServiceBaseUrl))
            .AddHttpMessageHandler<BearerTokenForwardingHandler>();

        builder.Services
            .AddFlowChatHttpClient<IChatServiceClient, ChatServiceClient>((_, client) =>
                client.BaseAddress = new Uri(servicesSettings.ChatServiceBaseUrl))
            .AddHttpMessageHandler<BearerTokenForwardingHandler>();

        builder.Services
            .AddFlowChatHttpClient<IPresenceServiceClient, PresenceServiceClient>((_, client) =>
            {
                client.BaseAddress = new Uri(servicesSettings.PresenceServiceBaseUrl);
                if (!string.IsNullOrWhiteSpace(servicesSettings.PresenceServiceInternalApiKey))
                {
                    client.DefaultRequestHeaders.Add(
                        PresenceServiceClient.ApiKeyHeaderName,
                        servicesSettings.PresenceServiceInternalApiKey);
                }
            });

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
            app.LogSwaggerEndpointOnStarted();
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

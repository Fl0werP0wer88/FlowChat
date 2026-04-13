using System.Text;
using System.Text.Json.Serialization;
using FlowChat.Shared.API;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FlowChat.RealtimeService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var apiSettingsManager = new ApiSettingsManager(builder.Configuration);
        var apiRuntimeSettings = apiSettingsManager.GetApiRuntimeSettings();
        var jwtSettings = apiSettingsManager.GetJwtSettings();
        var realtimeConnectionsSettings = apiSettingsManager.GetRealtimeConnectionsSettings();
        var realtimeConnectionProducerOptions = new KafkaSettingsManager(builder.Configuration).GetRealtimeConnectionProducerOptions();
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

        if (string.IsNullOrWhiteSpace(realtimeConnectionsSettings.RedisConnectionString))
        {
            throw new InvalidOperationException("Missing configuration value: ConnectionStrings:Redis.");
        }

        if (string.IsNullOrWhiteSpace(realtimeConnectionsSettings.InstanceId))
        {
            throw new InvalidOperationException("Missing configuration value: RealtimeConnections:InstanceId.");
        }

        if (realtimeConnectionsSettings.ConnectionTtl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("RealtimeConnections:ConnectionTtl must be greater than zero.");
        }

        if (realtimeConnectionsSettings.RefreshInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("RealtimeConnections:RefreshInterval must be greater than zero.");
        }

        if (realtimeConnectionsSettings.RefreshInterval >= realtimeConnectionsSettings.ConnectionTtl)
        {
            throw new InvalidOperationException("RealtimeConnections:RefreshInterval must be smaller than RealtimeConnections:ConnectionTtl.");
        }

        if (string.IsNullOrWhiteSpace(realtimeConnectionProducerOptions.BootstrapServers))
        {
            throw new InvalidOperationException("Missing configuration value: Kafka:RealtimeConnectionProducer:BootstrapServers.");
        }

        if (string.IsNullOrWhiteSpace(realtimeConnectionProducerOptions.Topic))
        {
            throw new InvalidOperationException("Missing configuration value: Kafka:RealtimeConnectionProducer:Topic.");
        }

        builder.Services.AddApiApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddApiSilverbackMessaging(builder.Configuration);
        builder.Services.AddScoped<IRealtimeClientDispatcher, SignalRRealtimeClientDispatcher>();
        builder.AddFlowChatOpenTelemetry(typeof(ApplicationServiceRegistration).Assembly);

        builder.Services
            .AddAuthentication(options =>
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
            });
        builder.Services.AddAuthorization();
        builder.Services.AddSignalR()
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins([apiRuntimeSettings.ApiUrl, apiRuntimeSettings.BlazorUrl])
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowCredentials()));
        builder.Services.AddSwaggerGen();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCors("open");
        app.UseFlowChatGlobalExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

        return app;
    }
}


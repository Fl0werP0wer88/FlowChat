using System.Text;
using System.Text.Json.Serialization;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.Kafka;
using FlowChat.Shared.API;
using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FlowChat.RealtimeService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var settingsProvider = new AppSettingsProvider(builder.Configuration);
        var jwtSettings = settingsProvider.GetSection<JwtSettingsSection>();
        var realtimeConnectionsSettings = settingsProvider.GetSection<RealtimeConnectionsSettingsSection>();
        realtimeConnectionsSettings.RedisConnectionString =
            builder.Configuration.GetConnectionString(RealtimeConnectionsSettingsSection.RedisConnectionStringName)
            ?? realtimeConnectionsSettings.RedisConnectionString;
        var realtimeConnectionRegisteredProducerOptions =
            settingsProvider.GetSection<RealtimeConnectionRegisteredProducerSettingsSection>();
        var realtimeConnectionUnregisteredProducerOptions =
            settingsProvider.GetSection<RealtimeConnectionUnregisteredProducerSettingsSection>();
        var jwtKey = jwtSettings.Key;
        var jwtIssuer = jwtSettings.Issuer;
        var jwtAudience = jwtSettings.Audience;

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Key.");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Audience.");
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

        ValidateKafkaProducerOptions(
            realtimeConnectionRegisteredProducerOptions.SectionName,
            realtimeConnectionRegisteredProducerOptions.BootstrapServers,
            realtimeConnectionRegisteredProducerOptions.Topic);
        ValidateKafkaProducerOptions(
            realtimeConnectionUnregisteredProducerOptions.SectionName,
            realtimeConnectionUnregisteredProducerOptions.BootstrapServers,
            realtimeConnectionUnregisteredProducerOptions.Topic);

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
        builder.Services.AddSwaggerGen();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
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

    private static void ValidateKafkaProducerOptions(string sectionName, string bootstrapServers, string topic)
    {
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            throw new InvalidOperationException($"Missing configuration value: {sectionName}:BootstrapServers.");
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new InvalidOperationException($"Missing configuration value: {sectionName}:Topic.");
        }
    }
}


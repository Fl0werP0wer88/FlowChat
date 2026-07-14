using System.Text.Json.Serialization;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Persistence;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.Shared.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var realtimeConnectionsSettings = builder.Configuration.GetSection(new RealtimeConnectionsSettingsSection().SectionName)
            .Get<RealtimeConnectionsSettingsSection>() ?? new RealtimeConnectionsSettingsSection();
        realtimeConnectionsSettings.RedisConnectionString =
            builder.Configuration.GetConnectionString(RealtimeConnectionsSettingsSection.RedisConnectionStringName)
            ?? realtimeConnectionsSettings.RedisConnectionString;

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

        builder.Services.AddApiApplicationServices();
        builder.Services.AddApiInfrastructureServices(builder.Configuration);
        builder.Services.AddApiPersistenceServices(builder.Configuration);
        builder.Services.AddScoped<IRealtimeClientDispatcher, SignalRRealtimeClientDispatcher>();
        builder.Services.AddScoped<IRealtimeGroupManager, SignalRRealtimeGroupManager>();
        builder.AddFlowChatOpenTelemetry(typeof(ApiApplicationServiceRegistration).Assembly);

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
            });
        builder.Services.AddSingleton<ChatHubExceptionFilter>();
        builder.Services.AddSignalR(options => options.AddFilter<ChatHubExceptionFilter>())
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddFlowChatSwaggerWithBearer();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseFlowChatGlobalExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.LogSwaggerEndpointOnStarted();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

        return app;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var context = new AppDbContextFactory().CreateDbContext([]);
        await context.Database.MigrateAsync();
    }
}

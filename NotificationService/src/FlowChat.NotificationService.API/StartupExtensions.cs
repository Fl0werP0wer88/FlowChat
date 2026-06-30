using FlowChat.Shared.API;
using FlowChat.NotificationService.Application;
using FlowChat.NotificationService.Infrastructure.Configuration.Settings;
using FlowChat.NotificationService.Infrastructure;
using FlowChat.NotificationService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiApplicationServices();
        builder.Services.AddApiInfrastructureServices(builder.Configuration);
        builder.Services.AddApiPersistenceServices(builder.Configuration);
        builder.AddFlowChatOpenTelemetry(typeof(ApiApplicationServiceRegistration).Assembly);

        builder.Services.AddFlowChatJwtAuthentication(builder.Configuration);
        builder.Services.AddControllers();
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


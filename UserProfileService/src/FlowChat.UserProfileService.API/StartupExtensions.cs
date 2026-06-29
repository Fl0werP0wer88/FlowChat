using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FlowChat.UserProfileService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiApplicationServices();
        builder.Services.AddApiInfrastructureServices(builder.Configuration);
        builder.Services.AddApiPersistenceServices(builder.Configuration);
        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .SetApplicationName("FlowChat.UserProfileService");
        builder.Services.AddApiSilverbackMessaging(builder.Configuration);
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



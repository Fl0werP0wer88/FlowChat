using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FlowChat.UserProfileService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var apiSettingsManager = new ApiSettingsManager(builder.Configuration);
        var apiRuntimeSettings = apiSettingsManager.GetApiRuntimeSettings();

        builder.Services.AddApiApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.AddApiSilverbackMessaging(builder.Configuration);
        builder.AddFlowChatOpenTelemetry(typeof(ApplicationServiceRegistration).Assembly);

        builder.Services.AddControllers();

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins([apiRuntimeSettings.ApiUrl, apiRuntimeSettings.BlazorUrl])
        .AllowAnyMethod()
        .SetIsOriginAllowed(pol => true) // DevNote To be removed whe UI address established
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



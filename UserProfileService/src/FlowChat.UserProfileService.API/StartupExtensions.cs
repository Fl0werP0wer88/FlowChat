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
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapControllers();
        return app;
    }

    public static async Task ResetDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using var context = new AppDbContextFactory().CreateDbContext([]);
            if (app.Services.GetRequiredService<IApiSettingsManager>().GetApiRuntimeSettings().DropDatabaseOnStartup)
            {
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            app.Logger.LogError(
                exception,
                "Failed to reset or migrate the UserProfileService database during startup.");

            throw;
        }
    }
}


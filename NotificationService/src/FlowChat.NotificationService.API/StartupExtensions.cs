using FlowChat.NotificationService.Application;
using FlowChat.NotificationService.Infrastructure;
using FlowChat.NotificationService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddPersistenceServices(builder.Configuration);

        builder.Services.AddControllers();

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins(
                        [
                            builder.Configuration["ApiUrl"] ?? "https://localhost:5000",
                            builder.Configuration["BlazorUrl"] ?? "https://localhost:5010"
                        ])
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
            if (app.Configuration.GetValue<bool>("FlowChat:DropDatabaseOnStartup"))
            {
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();
        }
        catch (Exception)
        {
            // TODO: add proper logging.
        }
    }
}

using FlowChat.AuthService.Application;
using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FlowChat.AuthService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddWolverineMessaging();

        builder.Services
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration)
        .AddAPIPersistenceServices(builder.Configuration)
        .AddAPIServices(builder.Configuration);


        builder.Services.AddControllers();

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins([builder.Configuration["ApiUrl"] ?? "https://localhost:5000",
                    builder.Configuration["BlazorUrl"] ?? "https://localhost:5010"])
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
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
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
            throw;
        }
    }
}


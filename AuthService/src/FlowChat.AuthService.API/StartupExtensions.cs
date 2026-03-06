using FlowChat.AuthService.Application;
using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
namespace FlowChat.AuthService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddWolverineMessaging();
        builder.Host.UseResourceSetupOnStartup();
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

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.SetConnectionString(ResolveMigrationConnectionString(app.Configuration));

        if (app.Configuration.GetValue<bool>("FlowChat:DropDatabaseOnStartup"))
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.MigrateAsync();
    }

    private static string ResolveMigrationConnectionString(IConfiguration configuration)
    {
        return Environment.GetEnvironmentVariable("AUTH_DB_MIGRATION_CONNECTION_STRING")
            ?? configuration.GetConnectionString("AuthDbMigration")
            ?? "Host=localhost;Port=5432;Database=flowchat_auth_db;Username=flowchat_migrator;Password=flowchat_migrator_pw;";
    }
}


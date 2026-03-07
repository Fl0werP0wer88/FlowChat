using FlowChat.AuthService.Application;
using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.AuthService.Api;

public static class StartupExtensions
{
    private const string DuplicateTableSqlState = "42P07";
    private const string DuplicateObjectSqlState = "42710";

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration)
        .AddAPIPersistenceServices(builder.Configuration)
        .AddApiSilverbackMessaging(builder.Configuration)
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

        var dropDatabaseOnStartup = app.Configuration.GetValue<bool>("FlowChat:DropDatabaseOnStartup");

        await using var context = new AppDbContextFactory().CreateDbContext([]);
        if (dropDatabaseOnStartup)
        {
            app.Logger.LogWarning(
                "FlowChat:DropDatabaseOnStartup is enabled for AuthService. Resetting local dev database. Disable this flag again after the next successful startup.");

            await context.Database.EnsureDeletedAsync();
        }

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (PostgresException exception) when (!dropDatabaseOnStartup && IsSchemaDrift(exception))
        {
            app.Logger.LogError(
                exception,
                "AuthService detected local database schema drift. Set FlowChat:DropDatabaseOnStartup=true, start once to recreate flowchat_auth_db, then set it back to false.");

            throw new InvalidOperationException(
                "Local AuthService database schema does not match migrations on this branch. " +
                "This usually means flowchat_auth_db was created from a different branch. " +
                "Set FlowChat:DropDatabaseOnStartup=true, start AuthService once so flowchat_migrator recreates the database, then set the flag back to false. " +
                "Alternatively, delete flowchat_auth_db manually and start the service again.",
                exception);
        }
    }

    private static bool IsSchemaDrift(PostgresException exception) =>
        exception.SqlState is DuplicateTableSqlState or DuplicateObjectSqlState;
}


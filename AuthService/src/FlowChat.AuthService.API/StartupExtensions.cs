using FlowChat.API.Abstractions;
using FlowChat.AuthService.Application;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.AuthService.Api;

public static class StartupExtensions
{
    private const string DuplicateTableSqlState = "42P07";
    private const string DuplicateObjectSqlState = "42710";

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var apiSettingsManager = new ApiSettingsManager(builder.Configuration);
        var apiRuntimeSettings = apiSettingsManager.GetApiRuntimeSettings();

        builder.Services
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration)
        .AddAPIPersistenceServices(builder.Configuration)
        .AddApiSilverbackMessaging(builder.Configuration)
        .AddAPIServices(builder.Configuration);
        builder.Services.AddAutoMapper(
            (Action<AutoMapper.IMapperConfigurationExpression>?)null,
            typeof(StartupExtensions).Assembly);
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

        var apiRuntimeSettings = app.Services.GetRequiredService<IApiSettingsManager>().GetApiRuntimeSettings();
        var dropDatabaseOnStartup = apiRuntimeSettings.DropDatabaseOnStartup;
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
            await EnsureAppRoleCrudAccessAsync(app, context);
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

    private static async Task EnsureAppRoleCrudAccessAsync(WebApplication app, AppDbContext context)
    {
        var appConnectionString = app.Services
            .GetRequiredService<IApiSettingsManager>()
            .GetApiRuntimeSettings()
            .AuthDbConnectionString;
        if (string.IsNullOrWhiteSpace(appConnectionString))
        {
            throw new InvalidOperationException("Missing connection string: AuthDb.");
        }
        var appConnectionStringBuilder = new NpgsqlConnectionStringBuilder(appConnectionString);
        var migratorConnectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Missing migrator connection string for AuthService database reset.");
        var migratorConnectionStringBuilder = new NpgsqlConnectionStringBuilder(migratorConnectionString);

        var databaseName = QuoteIdentifier(migratorConnectionStringBuilder.Database);
        var appRoleName = QuoteIdentifier(appConnectionStringBuilder.Username);
        var migratorRoleName = QuoteIdentifier(migratorConnectionStringBuilder.Username);

        await context.Database.ExecuteSqlRawAsync($"GRANT CONNECT ON DATABASE {databaseName} TO {appRoleName};");
        await context.Database.ExecuteSqlRawAsync($"GRANT USAGE ON SCHEMA public TO {appRoleName};");
        await context.Database.ExecuteSqlRawAsync($"REVOKE CREATE ON SCHEMA public FROM {appRoleName};");
        await context.Database.ExecuteSqlRawAsync(
            $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO {appRoleName};");
        await context.Database.ExecuteSqlRawAsync(
            $"GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO {appRoleName};");
        await context.Database.ExecuteSqlRawAsync(
            $"ALTER DEFAULT PRIVILEGES FOR ROLE {migratorRoleName} IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {appRoleName};");
        await context.Database.ExecuteSqlRawAsync(
            $"ALTER DEFAULT PRIVILEGES FOR ROLE {migratorRoleName} IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO {appRoleName};");

        app.Logger.LogInformation(
            "Ensured CRUD grants for AuthService app role {AppRole} on database {DatabaseName}.",
            appConnectionStringBuilder.Username,
            migratorConnectionStringBuilder.Database);
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"")}\"";
}


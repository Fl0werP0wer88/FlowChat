using FlowChat.NotificationService.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.UnitTests.Persistence;

public sealed class AppDbContextFactoryTests
{
    private const string MigrationConnectionStringVariableName =
        "NOTIFICATION_DB_MIGRATION_CONNECTION_STRING";
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=flowchat_notification_db;Username=flowchat_migrator;Password=flowchat_migrator_pw";
    private static readonly object EnvironmentLock = new();

    [Fact]
    public void CreateDbContext_WhenMigrationConnectionStringIsSet_UsesEnvironmentConnectionString()
    {
        const string migrationConnectionString =
            "Host=test-host;Port=5432;Database=test_notification_db;Username=test_user;Password=test_password";

        using var dbContext = CreateDbContext(migrationConnectionString);

        dbContext.Database.GetConnectionString().Should().Be(migrationConnectionString);
    }

    [Fact]
    public void CreateDbContext_WhenMigrationConnectionStringIsMissing_UsesDefaultConnectionString()
    {
        using var dbContext = CreateDbContext(null);

        dbContext.Database.GetConnectionString().Should().Be(DefaultConnectionString);
    }

    private static AppDbContext CreateDbContext(string? migrationConnectionString)
    {
        lock (EnvironmentLock)
        {
            var originalConnectionString = Environment.GetEnvironmentVariable(
                MigrationConnectionStringVariableName
            );

            try
            {
                Environment.SetEnvironmentVariable(
                    MigrationConnectionStringVariableName,
                    migrationConnectionString
                );

                var factory = new AppDbContextFactory();
                return factory.CreateDbContext([]);
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    MigrationConnectionStringVariableName,
                    originalConnectionString
                );
            }
        }
    }
}

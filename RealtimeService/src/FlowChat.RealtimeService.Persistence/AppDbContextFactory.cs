using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowChat.RealtimeService.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("REALTIME_DB_MIGRATION_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=flowchat_realtime_db;Username=flowchat_migrator;Password=flowchat_migrator_pw";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}

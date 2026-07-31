using System.Data.Common;
using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.RealtimeService.Persistence;

public class AppDbContext : DbContext
{
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Silverback uses the active connection so offset and outbox writes share the business transaction
    public AppDbContext(DbConnection connection)
        : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options)
    {
    }

    public DbSet<RealtimeGroupMembershipReadModel> RealtimeGroupMembershipReadModels => Set<RealtimeGroupMembershipReadModel>();

    public DbSet<RealtimeGroupMembershipRevisionTrackerReadModel> RealtimeGroupMembershipRevisionTrackerReadModels => Set<RealtimeGroupMembershipRevisionTrackerReadModel>();

    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

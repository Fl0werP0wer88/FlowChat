using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.RealtimeService.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RealtimeGroupMembershipReadModel> RealtimeGroupMembershipReadModels => Set<RealtimeGroupMembershipReadModel>();

    public DbSet<RealtimeGroupMembershipVersionTrackerReadModel> RealtimeGroupMembershipVersionTrackerReadModels => Set<RealtimeGroupMembershipVersionTrackerReadModel>();

    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

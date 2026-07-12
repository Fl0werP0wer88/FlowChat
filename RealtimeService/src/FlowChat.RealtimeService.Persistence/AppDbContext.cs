using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RealtimeGroupMembershipReadModel> RealtimeGroupMembershipReadModels => Set<RealtimeGroupMembershipReadModel>();

    public DbSet<RealtimeGroupMembershipVersionTrackerReadModel> RealtimeGroupMembershipVersionTrackerReadModels => Set<RealtimeGroupMembershipVersionTrackerReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

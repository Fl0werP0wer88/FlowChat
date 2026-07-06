using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RealtimeGroupMembership> RealtimeGroupMemberships => Set<RealtimeGroupMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

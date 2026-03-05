using FlowChat.NotificationService.Domain.Common;
using FlowChat.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.NotificationService.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    // {
    //     foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
    //     {
    //         switch (entry.State)
    //         {
    //             case EntityState.Added:
    //                 entry.Entity.CreatedDate = DateTime.UtcNow;
    //                 break;
    //             case EntityState.Modified:
    //                 entry.Entity.LastModifiedDate = DateTime.UtcNow;
    //                 break;
    //         }
    //     }

    //     return base.SaveChangesAsync(cancellationToken);
    // }
}

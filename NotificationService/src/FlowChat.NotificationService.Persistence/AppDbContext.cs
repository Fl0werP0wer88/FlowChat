using FlowChat.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var entry in ChangeTracker.Entries<Notification>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    var now = DateTimeOffset.UtcNow;
                    entry.Property(nameof(Notification.CreatedAtUtc)).CurrentValue = now;
                    entry.Property(nameof(Notification.LastModifiedAtUtc)).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(Notification.LastModifiedAtUtc)).CurrentValue = DateTimeOffset.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

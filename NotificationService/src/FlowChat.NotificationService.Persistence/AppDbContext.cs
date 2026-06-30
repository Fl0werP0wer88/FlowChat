using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Persistence.Entities;
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
    public DbSet<NotificationReadEntity> NotificationReads => Set<NotificationReadEntity>();
    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

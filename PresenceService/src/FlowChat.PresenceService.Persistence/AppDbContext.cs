using FlowChat.PresenceService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.PresenceService.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ContactObserverProjectionEntity> ContactObserverProjections => Set<ContactObserverProjectionEntity>();
    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

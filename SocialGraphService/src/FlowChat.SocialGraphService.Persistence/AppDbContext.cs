using System.Data.Common;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.SocialGraphService.Persistence;

public class AppDbContext : DbContext
{
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
    {
    }

    public AppDbContext(DbConnection connection)
        : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options)
    {
    }

    public DbSet<Contact> Contacts { get; set; }
    public DbSet<ContactReadEntity> ContactReads { get; set; }
    public DbSet<UserProfileReadModelEntity> UserProfileProjections { get; set; }
    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();
    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Setting default schema for tables creation.
        // modelBuilder.HasDefaultSchema("FlowChat");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    // {
    //     foreach (var entry in ChangeTracker.Entries<EntityBase>())
    //     {
    //         switch (entry.State)
    //         {
    //             case EntityState.Added:
    //                 break;
    //             case EntityState.Modified:
    //                 entry.Property(nameof(EntityBase.LastModifiedAtUtc)).CurrentValue = DateTimeOffset.UtcNow;
    //                 break;
    //         }
    //     }

    //     return base.SaveChangesAsync(cancellationToken);
    // }
}



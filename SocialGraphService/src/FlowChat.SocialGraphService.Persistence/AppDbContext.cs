using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
    {
    }

    public DbSet<Contact> Contacts { get; set; }
    public DbSet<UserProfileReadModelEntity> UserProfileReadModels { get; set; }

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


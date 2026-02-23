using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Persistence.Identity;
using FlowChat.AuthService.Persistence.Outbox;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.AuthService.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(message => message.Id);

            builder.Property(message => message.Type)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(message => message.Topic)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(message => message.Key)
                .HasMaxLength(200);

            builder.Property(message => message.Content)
                .IsRequired()
                .HasColumnType("jsonb");

            builder.Property(message => message.Headers)
                .HasColumnType("jsonb");

            builder.Property(message => message.OccurredOnUtc)
                .IsRequired();

            builder.Property(message => message.Error)
                .HasColumnType("text");

            builder.HasIndex(message => new { message.ProcessedOnUtc, message.NextRetryOnUtc });
            builder.HasIndex(message => message.OccurredOnUtc);
            builder.HasIndex(message => message.Topic);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedDate = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

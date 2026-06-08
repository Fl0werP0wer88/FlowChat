using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.PresenceService.Persistence.Configuration.Entities;

public sealed class ContactObserverProjectionConfiguration : IEntityTypeConfiguration<ContactObserverReadModelEntity>
{
    public void Configure(EntityTypeBuilder<ContactObserverReadModelEntity> builder)
    {
        builder.ToTable("ContactObserverReadModel", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "chk_contact_observer_projection_different_users",
                "\"ObservedUserId\" <> \"ObserverUserId\"");
        });

        builder.HasKey(x => new { x.ObservedUserId, x.ObserverUserId });

        builder.Property(x => x.SourceVersion);

        builder.Property(x => x.SourceCreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.SourceLastModifiedAtUtc)
            .IsRequired();

        builder.Property(x => x.SourceDeletedAtUtc);

        builder.Property(x => x.DeletedAt)
            .HasComputedColumnSql($"\"{nameof(ReadModelEntityBase.SourceDeletedAtUtc)}\"", stored: true);

        builder.HasIndex(x => x.ObservedUserId)
            .HasDatabaseName("ix_contact_observer_projection_observed_user_id");
    }
}

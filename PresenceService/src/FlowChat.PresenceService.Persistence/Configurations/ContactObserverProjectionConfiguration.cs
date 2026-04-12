using FlowChat.PresenceService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.PresenceService.Persistence.Configurations;

public sealed class ContactObserverProjectionConfiguration : IEntityTypeConfiguration<ContactObserverProjectionEntity>
{
    public void Configure(EntityTypeBuilder<ContactObserverProjectionEntity> builder)
    {
        builder.ToTable("ContactObserverProjection", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "chk_contact_observer_projection_different_users",
                "\"ObservedUserId\" <> \"ObserverUserId\"");
        });

        builder.HasKey(x => new { x.ObservedUserId, x.ObserverUserId });

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc);

        builder.HasIndex(x => x.ObservedUserId)
            .HasDatabaseName("ix_contact_observer_projection_observed_user_id");
    }
}

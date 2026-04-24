using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.PresenceService.Persistence.Configuration.Entities;

public sealed class UserPresencePreferencesConfiguration : IEntityTypeConfiguration<UserPresencePreferences>
{
    public void Configure(EntityTypeBuilder<UserPresencePreferences> builder)
    {
        builder.ToTable("UserPresencePreferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("UserId")
            .HasConversion(x => x.Value, x => Id<UserPresencePreferences>.FromGuid(x));

        builder.Property(x => x.PreferredStatus).IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Ignore(x => x.UserId);
        builder.Ignore(x => x.DomainEvents);
    }
}

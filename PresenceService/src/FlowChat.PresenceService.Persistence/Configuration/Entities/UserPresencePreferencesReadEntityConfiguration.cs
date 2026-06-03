using FlowChat.PresenceService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.PresenceService.Persistence.Configuration.Entities;

public sealed class UserPresencePreferencesReadEntityConfiguration : IEntityTypeConfiguration<UserPresencePreferencesReadEntity>
{
    public void Configure(EntityTypeBuilder<UserPresencePreferencesReadEntity> builder)
    {
        builder.HasKey(x => x.UserId);
        builder.ToView("UserPresencePreferences");

        builder.Property(x => x.UserId)
            .HasColumnName("UserId");

        builder.Property(x => x.DeletedAt);
    }
}

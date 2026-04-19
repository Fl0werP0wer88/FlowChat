using FlowChat.PresenceService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.PresenceService.Persistence.Configuration.Entities;

public sealed class UserPresencePreferencesConfiguration : IEntityTypeConfiguration<UserPresencePreferencesEntity>
{
    public void Configure(EntityTypeBuilder<UserPresencePreferencesEntity> builder)
    {
        builder.ToTable("UserPresencePreferences");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.PreferredStatus).IsRequired();
        builder.Property(x => x.LastModifiedAtUtc).IsRequired();
    }
}

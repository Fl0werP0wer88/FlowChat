using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class UserProfileProjectionEntityConfiguration : IEntityTypeConfiguration<UserProfileProjectionEntity>
{
    public void Configure(EntityTypeBuilder<UserProfileProjectionEntity> builder)
    {
        builder.ToTable("UserProfileProjections");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FriendlyUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(256);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();
    }
}

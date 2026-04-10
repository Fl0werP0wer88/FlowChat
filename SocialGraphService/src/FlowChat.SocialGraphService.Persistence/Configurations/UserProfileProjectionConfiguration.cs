using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.SocialGraphService.Persistence.Configurations;

public sealed class UserProfileProjectionConfiguration : IEntityTypeConfiguration<UserProfileProjectionEntityDto>
{
    public void Configure(EntityTypeBuilder<UserProfileProjectionEntityDto> builder)
    {
        builder.ToTable("UserProfileProjection");

        builder.HasKey(x => x.UserProfileId);

        builder.Property(x => x.FriendlyUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Property(x => x.Organization)
            .HasMaxLength(200);

        builder.Property(x => x.MainEmail)
            .HasMaxLength(256);

        builder.Property(x => x.MainEmailIsConfirmed);

        builder.Property(x => x.MainEmailIsVisible);

        builder.Property(x => x.MainPhone)
            .HasMaxLength(32);

        builder.Property(x => x.MainPhoneIsConfirmed);

        builder.Property(x => x.MainPhoneIsVisible);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.Bio)
            .HasMaxLength(2000);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc);

        builder.HasIndex(x => x.FriendlyUserId)
            .HasDatabaseName("ix_user_profile_projection_friendly_user_id");
    }
}


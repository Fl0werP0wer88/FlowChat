using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.SocialGraphService.Persistence.Configuration.Entities;

public sealed class UserProfileProjectionConfiguration : IEntityTypeConfiguration<UserProfileReadModelEntity>
{
    public void Configure(EntityTypeBuilder<UserProfileReadModelEntity> builder)
    {
        builder.ToTable("UserProfileReadModel");

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

        builder.Property(x => x.SourceVersion)
            .IsRequired();

        builder.Property(x => x.SourceCreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.SourceLastModifiedAtUtc)
            .IsRequired();

        builder.Property(x => x.SourceDeletedAtUtc);

        // Generated column mirroring SourceDeletedAtUtc so ReadRepositoryBase.Active's DeletedAt == null filter is translatable to SQL without a redundant write path
        builder.Property(x => x.DeletedAt)
            .HasComputedColumnSql($"\"{nameof(ReadModelEntityBase.SourceDeletedAtUtc)}\"", stored: true);

        builder.HasIndex(x => x.FriendlyUserId)
            .HasDatabaseName("ix_user_profile_projection_friendly_user_id");
    }
}


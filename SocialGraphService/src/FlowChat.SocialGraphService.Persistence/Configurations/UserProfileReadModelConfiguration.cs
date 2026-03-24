using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.SocialGraphService.Persistence.Configurations;

public sealed class UserProfileReadModelConfiguration : IEntityTypeConfiguration<UserProfileReadModelEntity>
{
    public void Configure(EntityTypeBuilder<UserProfileReadModelEntity> builder)
    {
        builder.ToTable("UserProfileReadModel");

        builder.HasKey(x => x.UserProfileId);

        builder.Property(x => x.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MainEmail)
            .HasMaxLength(256);

        builder.Property(x => x.MainPhone)
            .HasMaxLength(32);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.Bio)
            .HasMaxLength(2000);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsEmailVisible)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsPhoneVisible)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc);

        builder.HasIndex(x => x.UserName)
            .HasDatabaseName("ix_user_profile_read_model_user_name");
    }
}

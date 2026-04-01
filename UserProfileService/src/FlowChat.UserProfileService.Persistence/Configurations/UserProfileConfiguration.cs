using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<UserProfile>.FromGuid(x));

        builder.Property(x => x.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NormalizedUserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Bio)
            .HasMaxLength(500);

        builder.Property(x => x.IsEmailVisible)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsPhoneVisible)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc);

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.HasMany(x => x.Emails)
            .WithOne()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Phones)
            .WithOne()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Emails)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Phones)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}


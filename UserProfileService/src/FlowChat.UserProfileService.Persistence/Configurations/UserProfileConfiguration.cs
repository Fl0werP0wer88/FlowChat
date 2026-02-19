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

        builder.Property(x => x.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Bio)
            .HasMaxLength(500);

        builder.HasIndex(x => x.UserName)
            .IsUnique();
    }
}

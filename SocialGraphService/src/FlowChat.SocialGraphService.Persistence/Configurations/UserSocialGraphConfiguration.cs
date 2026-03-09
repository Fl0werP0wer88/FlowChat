using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.SocialGraphService.Persistence.Configurations;

public sealed class UserSocialGraphConfiguration : IEntityTypeConfiguration<UserSocialGraphEntity>
{
    public void Configure(EntityTypeBuilder<UserSocialGraphEntity> builder)
    {
        builder.ToTable("UserSocialGraphs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Property(x => x.Login)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(32);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.IsPhoneVisible)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsEmailVisible)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("uq_user_social_graph_user_id");
    }
}

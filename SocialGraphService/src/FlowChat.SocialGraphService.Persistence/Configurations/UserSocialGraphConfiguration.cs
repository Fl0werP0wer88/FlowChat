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

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("uq_user_social_graph_user_id");

        builder.HasMany(x => x.Contacts)
            .WithOne(x => x.UserSocialGraph)
            .HasForeignKey(x => x.UserSocialGraphId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Invitations)
            .WithOne(x => x.UserSocialGraph)
            .HasForeignKey(x => x.UserSocialGraphId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

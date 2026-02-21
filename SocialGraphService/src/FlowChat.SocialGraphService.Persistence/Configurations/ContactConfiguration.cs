using FlowChat.SocialGraphService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.SocialGraphService.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts", t =>
        {
            t.HasCheckConstraint("chk_different_users", "\"UserId1\" <> \"UserId2\"");
            t.HasCheckConstraint("chk_user_order", "\"UserId1\" < \"UserId2\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsBlocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId1, x.UserId2 })
            .IsUnique()
            .HasDatabaseName("uq_contact_pair");
    }
}

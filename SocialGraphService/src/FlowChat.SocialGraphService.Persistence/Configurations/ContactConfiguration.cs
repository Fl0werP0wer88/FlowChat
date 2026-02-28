using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<ContactEntity>
{
    public void Configure(EntityTypeBuilder<ContactEntity> builder)
    {
        builder.ToTable("Contacts", t =>
        {
            t.HasCheckConstraint("chk_different_users", "\"OwnerUserId\" <> \"ContactUserId\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsBlocked)
            .HasDefaultValue(false)
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

        builder.HasIndex(x => new { x.OwnerUserId, x.ContactUserId })
            .IsUnique()
            .HasDatabaseName("uq_contact_owner_contact");
    }
}

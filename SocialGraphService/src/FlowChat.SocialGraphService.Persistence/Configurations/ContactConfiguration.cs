using FlowChat.Domain.Abstractions;
using FlowChat.Domain.Abstractions.ValueObjects;
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
            t.HasCheckConstraint("chk_different_users", "\"OwnerUserId\" <> \"ContactUserId\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<Contact>.FromGuid(x));

        builder.Property(x => x.IsBlocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Property(x => x.DisplayedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasConversion(
                x => x == null ? null : x.Value,
                x => x == null ? null : PhoneNumber.Create(x))
            .HasMaxLength(32);

        builder.Property(x => x.Email)
            .HasConversion(
                x => x == null ? null : x.Value,
                x => x == null ? null : EmailAddress.Create(x))
            .HasMaxLength(256);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("CreatedDate");

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasColumnName("LastModifiedDate");

        builder.HasIndex(x => new { x.OwnerUserId, x.ContactUserId })
            .IsUnique()
            .HasDatabaseName("uq_contact_owner_contact");
    }
}

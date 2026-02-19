using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts", t =>
            t.HasCheckConstraint("CK_Contacts_Requester_Not_Addressee", "\"RequesterId\" <> \"AddresseeId\""));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(x => x.Requester)
            .WithMany(x => x.SentContacts)
            .HasForeignKey(x => x.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Addressee)
            .WithMany(x => x.ReceivedContacts)
            .HasForeignKey(x => x.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RequesterId, x.AddresseeId })
            .IsUnique();
    }
}

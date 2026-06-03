using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.SocialGraphService.Persistence.Configuration.Entities;

public sealed class ContactReadEntityConfiguration : IEntityTypeConfiguration<ContactReadEntity>
{
    public void Configure(EntityTypeBuilder<ContactReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("Contacts");

        builder.Property(x => x.DeletedAt);
    }
}

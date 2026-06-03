using FlowChat.UserProfileService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configuration.Entities;

public sealed class EmailReadEntityConfiguration : IEntityTypeConfiguration<EmailReadEntity>
{
    public void Configure(EntityTypeBuilder<EmailReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("Emails");

        builder.Property(x => x.DeletedAt);
    }
}

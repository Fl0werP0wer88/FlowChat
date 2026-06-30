using FlowChat.UserProfileService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configuration.Entities;

public sealed class PhoneReadEntityConfiguration : IEntityTypeConfiguration<PhoneReadEntity>
{
    public void Configure(EntityTypeBuilder<PhoneReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("Phones");

        builder.Property(x => x.DeletedAt);
    }
}

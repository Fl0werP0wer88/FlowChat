using FlowChat.UserProfileService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configuration.Entities;

public sealed class UserProfileReadEntityConfiguration : IEntityTypeConfiguration<UserProfileReadEntity>
{
    public void Configure(EntityTypeBuilder<UserProfileReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("UserProfiles");

        builder.Property(x => x.UserName)
            .HasColumnName("UserName");
    }
}

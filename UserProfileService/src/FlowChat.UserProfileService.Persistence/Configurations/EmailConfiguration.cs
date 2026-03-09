using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configurations;

public class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.ToTable("Emails");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<Email>.FromGuid(x));

        builder.Property(x => x.UserProfileId)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IsMain)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.UserProfileId, x.Address })
            .IsUnique()
            .HasDatabaseName("uq_email_user_profile_address");
    }
}

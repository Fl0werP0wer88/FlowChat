using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Shared.Domain.ValueObjects;
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
            .HasConversion(x => x.Value, x => Id<UserProfile>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.Address)
            .HasConversion(x => x.Value, x => EmailAddress.Create(x))
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.IsMain)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsAuth)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsConfirmed)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => x.Address)
            .IsUnique()
            .HasDatabaseName("uq_email_address");

        builder.HasIndex(x => x.UserProfileId)
            .IsUnique()
            .HasFilter("\"IsAuth\" = TRUE")
            .HasDatabaseName("uq_email_user_profile_auth");
    }
}


using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configurations;

public class PhoneConfiguration : IEntityTypeConfiguration<Phone>
{
    public void Configure(EntityTypeBuilder<Phone> builder)
    {
        builder.ToTable("Phones");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<Phone>.FromGuid(x));

        builder.Property(x => x.UserProfileId)
            .HasConversion(x => x.Value, x => Id<UserProfile>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.Number)
            .HasConversion(x => x.Value, x => PhoneNumber.Create(x))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IsMain)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.UserProfileId, x.Number })
            .IsUnique()
            .HasDatabaseName("uq_phone_user_profile_number");
    }
}


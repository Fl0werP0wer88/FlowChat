using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configurations;

public sealed class EmailVerificationRequestConfiguration : IEntityTypeConfiguration<EmailVerificationRequest>
{
    public void Configure(EntityTypeBuilder<EmailVerificationRequest> builder)
    {
        builder.ToTable("EmailVerificationRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<EmailVerificationRequest>.FromGuid(x));

        builder.Property(x => x.UserProfileId)
            .HasConversion(x => x.Value, x => Id<UserProfile>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.EmailId)
            .HasConversion(x => x.Value, x => Id<Email>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.Nonce)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.InvalidatedAtUtc);

        builder.Property(x => x.ConsumedAtUtc);

        builder.HasIndex(x => x.Nonce)
            .IsUnique()
            .HasDatabaseName("uq_email_verification_request_nonce");

        builder.HasIndex(x => x.EmailId)
            .HasDatabaseName("ix_email_verification_request_email_id");

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Email>()
            .WithMany()
            .HasForeignKey(x => x.EmailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

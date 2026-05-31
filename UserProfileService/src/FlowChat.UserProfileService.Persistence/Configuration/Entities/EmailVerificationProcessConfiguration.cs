using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.UserProfileService.Persistence.Configuration.Entities;

public sealed class EmailVerificationProcessConfiguration : IEntityTypeConfiguration<EmailVerificationProcess>
{
    public void Configure(EntityTypeBuilder<EmailVerificationProcess> builder)
    {
        builder.ToTable("EmailVerificationProcesses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<EmailVerificationProcess>.FromGuid(x));

        builder.Property(x => x.UserProfileId)
            .HasConversion(x => x.Value, x => Id<UserProfile>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.EmailId)
            .HasConversion(x => x.Value, x => Id<Email>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.EmailId)
            .IsUnique()
            .HasDatabaseName("uq_email_verification_process_email_id");

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Email>()
            .WithMany()
            .HasForeignKey(x => x.EmailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Requests)
            .WithOne()
            .HasForeignKey("EmailVerificationProcessId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Requests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class EmailVerificationRequestConfiguration : IEntityTypeConfiguration<EmailVerificationRequest>
{
    public void Configure(EntityTypeBuilder<EmailVerificationRequest> builder)
    {
        builder.ToTable("EmailVerificationRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<EmailVerificationRequest>.FromGuid(x));

        builder.Property<Id<EmailVerificationProcess>>("EmailVerificationProcessId")
            .HasConversion(x => x.Value, x => Id<EmailVerificationProcess>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.Nonce)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .HasUtcDateTimeOffsetConversion()
            .IsRequired();

        builder.Property(x => x.InvalidatedAtUtc)
            .HasNullableUtcDateTimeOffsetConversion();

        builder.Property(x => x.ConsumedAtUtc)
            .HasNullableUtcDateTimeOffsetConversion();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.HasIndex(x => x.Nonce)
            .IsUnique()
            .HasDatabaseName("uq_email_verification_request_nonce");

        builder.HasIndex("EmailVerificationProcessId")
            .HasDatabaseName("ix_email_verification_request_process_id");
    }
}

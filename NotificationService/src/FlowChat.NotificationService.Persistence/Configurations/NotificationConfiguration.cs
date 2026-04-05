using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.NotificationService.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<Notification>.FromGuid(x));

        builder.Property(x => x.Email)
            .HasConversion(x => x.Value, x => EmailAddress.Create(x))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        builder.Property(x => x.SourceMessageKey)
            .HasMaxLength(200);

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.SentAtUtc)
            .HasNullableUtcDateTimeOffsetConversion();

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SourceMessageKey)
            .IsUnique()
            .HasFilter("\"SourceMessageKey\" IS NOT NULL")
            .HasDatabaseName("uq_notification_source_message_key");
    }
}


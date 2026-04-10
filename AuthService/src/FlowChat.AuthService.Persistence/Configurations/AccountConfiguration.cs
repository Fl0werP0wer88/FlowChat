using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.AuthService.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    private const string NormalizedEmailPropertyName = "NormalizedEmail";

    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<Account>.FromGuid(x));

        builder.Property(x => x.Email)
            .HasConversion(x => x.Value, x => EmailAddress.Create(x))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property<string>(NormalizedEmailPropertyName)
            .HasColumnName(NormalizedEmailPropertyName)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.FriendlyUserId)
            .HasConversion(x => x.Value, x => FriendlyUserId.Create(x))
            .HasMaxLength(FriendlyUserId.MaxLength)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.SecurityStamp)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.AccessFailedCount)
            .IsRequired();

        builder.Property(x => x.IsEmailConfirmed)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(NormalizedEmailPropertyName)
            .IsUnique();
    }
}

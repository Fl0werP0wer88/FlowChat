using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.AuthService.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    private const string NormalizedEmailPropertyName = "NormalizedEmail";
    private const string NormalizedFriendlyUserIdPropertyName = "NormalizedFriendlyUserId";

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
            .HasMaxLength(100)
            .IsRequired();

        builder.Property<string>(NormalizedFriendlyUserIdPropertyName)
            .HasColumnName(NormalizedFriendlyUserIdPropertyName)
            .HasMaxLength(100)
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

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.Version);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.CreatedAtUtc);
        builder.Ignore(x => x.LastModifiedBy);
        builder.Ignore(x => x.LastModifiedAtUtc);

        builder.HasIndex(NormalizedEmailPropertyName)
            .IsUnique();

        builder.HasIndex(NormalizedFriendlyUserIdPropertyName)
            .IsUnique();
    }
}

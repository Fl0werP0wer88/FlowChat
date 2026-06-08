using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class UserProfileReadModelEntityConfiguration : IEntityTypeConfiguration<UserProfileReadModelEntity>
{
    public void Configure(EntityTypeBuilder<UserProfileReadModelEntity> builder)
    {
        builder.ToTable("UserProfileReadModel");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FriendlyUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.SourceVersion)
            .IsRequired();

        builder.Property(x => x.SourceCreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.SourceLastModifiedAtUtc)
            .IsRequired();

        builder.Property(x => x.SourceDeletedAtUtc);

        // Generated column mirroring SourceDeletedAtUtc so ReadRepositoryBase.Active's DeletedAt == null filter is translatable to SQL without a redundant write path
        builder.Property(x => x.DeletedAt)
            .HasComputedColumnSql($"\"{nameof(ReadModelEntityBase.SourceDeletedAtUtc)}\"", stored: true);
    }
}

using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ParticipantUserConfiguration : IEntityTypeConfiguration<ParticipantUser>
{
    public void Configure(EntityTypeBuilder<ParticipantUser> builder)
    {
        builder.ToTable("ParticipantUsers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ParticipantUser>.FromGuid(x));

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<Conversation>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(256);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.IsBlocked)
            .IsRequired();

        builder.Property(x => x.JoinedAtUtc)
            .HasUtcDateTimeOffsetConversion()
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique();
    }
}

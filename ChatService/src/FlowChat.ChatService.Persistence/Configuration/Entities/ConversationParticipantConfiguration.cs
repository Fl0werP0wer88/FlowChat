using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationParticipantConfiguration
    : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipantsV2");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ConversationParticipant>.FromGuid(x));

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.ConversationType)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(x => x.Value, x => Id<UserProfileMarker>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.DuetPartnerUserId)
            .HasConversion(
                x => x == null ? (Guid?)null : x.Value,
                x => x == null ? null : Id<UserProfileMarker>.FromGuid(x.Value));

        builder.Property(x => x.DisplayName).HasMaxLength(256);
        builder.Property(x => x.IsBlocked).IsRequired();
        builder.Property(x => x.IsMuted).IsRequired();
        builder.Property(x => x.IsHidden).IsRequired();
        builder.Property(x => x.JoinedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.LastReadMessageSequenceNum).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.LastModifiedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.LastModifiedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.DeletedAt).HasNullableUtcDateTimeOffsetConversion();
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DomainEvents);

        builder.HasOne<ConversationV2>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasIndex(x => new { x.ConversationId, x.IsHidden })
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}

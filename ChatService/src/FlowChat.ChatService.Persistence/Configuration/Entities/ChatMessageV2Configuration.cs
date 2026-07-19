using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ChatMessageV2Configuration : IEntityTypeConfiguration<ChatMessageV2>
{
    public void Configure(EntityTypeBuilder<ChatMessageV2> builder)
    {
        builder.ToTable("ChatMessagesV2");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ChatMessageV2>.FromGuid(x));

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.SenderUserId)
            .HasConversion(x => x.Value, x => Id<UserProfileMarker>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.Text).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.SentAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.DeliveredAtUtc).HasNullableUtcDateTimeOffsetConversion();
        builder.Property(x => x.SequenceNum);
        builder.Property(x => x.DeliveryStatus).HasDefaultValue(DeliveryStatus.Pending).IsRequired();
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

        builder.HasIndex(x => new { x.ConversationId, x.SentAtUtc });
        builder.HasIndex(x => new { x.ConversationId, x.SequenceNum })
            .IsUnique()
            .HasFilter("\"SequenceNum\" IS NOT NULL");
    }
}

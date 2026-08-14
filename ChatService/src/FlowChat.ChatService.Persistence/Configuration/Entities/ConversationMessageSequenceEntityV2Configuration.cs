using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationMessageSequenceEntityV2Configuration
    : IEntityTypeConfiguration<ConversationMessageSequenceEntityV2>
{
    public void Configure(EntityTypeBuilder<ConversationMessageSequenceEntityV2> builder)
    {
        builder.ToTable("ConversationMessageSequencesV2");
        builder.HasKey(x => x.ConversationId);

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x));

        builder.Property(x => x.LastAssignedSequenceNum)
            .HasDefaultValue(0L)
            .IsRequired();

        builder.HasOne<ConversationV2>()
            .WithOne()
            .HasForeignKey<ConversationMessageSequenceEntityV2>(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

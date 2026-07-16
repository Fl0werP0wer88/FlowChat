using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationMessageSequenceEntityConfiguration
    : IEntityTypeConfiguration<ConversationMessageSequenceEntity>
{
    public void Configure(EntityTypeBuilder<ConversationMessageSequenceEntity> builder)
    {
        builder.ToTable("ConversationMessageSequences");
        builder.HasKey(x => x.ConversationId);

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<Conversation>.FromGuid(x));

        builder.Property(x => x.LastAssignedSequenceNum)
            .IsRequired();

        builder.HasOne<Conversation>()
            .WithOne()
            .HasForeignKey<ConversationMessageSequenceEntity>(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

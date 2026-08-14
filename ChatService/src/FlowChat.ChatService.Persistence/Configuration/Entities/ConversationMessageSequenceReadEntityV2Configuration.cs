using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationMessageSequenceReadEntityV2Configuration
    : IEntityTypeConfiguration<ConversationMessageSequenceReadEntityV2>
{
    public void Configure(EntityTypeBuilder<ConversationMessageSequenceReadEntityV2> builder)
    {
        builder.HasKey(x => x.ConversationId);
        builder.ToView("ConversationMessageSequencesV2");

        builder.Property(x => x.ConversationId);
        builder.Property(x => x.LastAssignedSequenceNum);
        builder.Ignore(x => x.DeletedAt);
    }
}

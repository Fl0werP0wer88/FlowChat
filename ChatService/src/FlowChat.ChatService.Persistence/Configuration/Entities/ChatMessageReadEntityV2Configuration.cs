using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ChatMessageReadEntityV2Configuration
    : IEntityTypeConfiguration<ChatMessageReadEntityV2>
{
    public void Configure(EntityTypeBuilder<ChatMessageReadEntityV2> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("ChatMessagesV2");

        builder.Property(x => x.Id);
        builder.Property(x => x.ConversationId);
        builder.Property(x => x.SenderUserId);
        builder.Property(x => x.Text);
        builder.Property(x => x.SentAtUtc);
        builder.Property(x => x.SequenceNum);
        builder.Property(x => x.DeletedAt);
    }
}

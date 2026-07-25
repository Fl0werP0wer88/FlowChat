using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class DuetConversationReadEntityV2Configuration
    : IEntityTypeConfiguration<DuetConversationReadEntityV2>
{
    public void Configure(EntityTypeBuilder<DuetConversationReadEntityV2> builder)
    {
        builder.HasKey(x => new { x.FirstUserId, x.SecondUserId });
        builder.ToView("DuetConversationsV2");

        builder.Property(x => x.FirstUserId);
        builder.Property(x => x.SecondUserId);
        builder.Property(x => x.ConversationId);
        builder.Property(x => x.DeletedAt);
    }
}

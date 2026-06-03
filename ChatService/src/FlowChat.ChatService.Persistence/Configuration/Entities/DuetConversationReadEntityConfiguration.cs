using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class DuetConversationReadEntityConfiguration : IEntityTypeConfiguration<DuetConversationReadEntity>
{
    public void Configure(EntityTypeBuilder<DuetConversationReadEntity> builder)
    {
        builder.HasKey(x => new { x.FirstUserId, x.SecondUserId });
        builder.ToView("DuetConversations");

        builder.Property(x => x.FirstUserId);
        builder.Property(x => x.SecondUserId);
        builder.Property(x => x.ConversationId);
        builder.Property(x => x.DeletedAt);
    }
}

using FlowChat.ChatService.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class DuetConversationConfiguration : IEntityTypeConfiguration<DuetConversation>
{
    public void Configure(EntityTypeBuilder<DuetConversation> builder)
    {
        builder.ToTable("DuetConversations");
        builder.HasKey(x => new { x.FirstUserId, x.SecondUserId });

        builder.Property(x => x.FirstUserId).IsRequired();
        builder.Property(x => x.SecondUserId).IsRequired();
        builder.Property(x => x.ConversationId).IsRequired();
    }
}

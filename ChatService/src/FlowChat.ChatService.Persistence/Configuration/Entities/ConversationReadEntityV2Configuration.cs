using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationReadEntityV2Configuration
    : IEntityTypeConfiguration<ConversationReadEntityV2>
{
    public void Configure(EntityTypeBuilder<ConversationReadEntityV2> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("ConversationsV2");

        builder.Property(x => x.Id);
        builder.Property(x => x.ConversationType);
        builder.Property(x => x.Name);
        builder.Property(x => x.DuetFirstUserId);
        builder.Property(x => x.DuetSecondUserId);
        builder.Property(x => x.Version);
        builder.Property(x => x.DeletedAt);
    }
}

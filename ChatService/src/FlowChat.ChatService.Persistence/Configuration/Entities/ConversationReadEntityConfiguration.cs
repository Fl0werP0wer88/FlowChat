using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationReadEntityConfiguration : IEntityTypeConfiguration<ConversationReadEntity>
{
    public void Configure(EntityTypeBuilder<ConversationReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("Conversations");

        builder.Property(x => x.Id);
        builder.Property(x => x.Type);
        builder.Property(x => x.Name);
        builder.Property(x => x.DeletedAt);
    }
}

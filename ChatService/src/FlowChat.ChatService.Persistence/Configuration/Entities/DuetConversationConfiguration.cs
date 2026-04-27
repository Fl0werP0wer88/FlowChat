using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.ReadModels;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DuetConversationReadModel = FlowChat.ChatService.Persistence.ReadModels.DuetConversation;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class DuetConversationConfiguration : IEntityTypeConfiguration<DuetConversationReadModel>
{
    public void Configure(EntityTypeBuilder<DuetConversationReadModel> builder)
    {
        builder.ToTable("DuetConversations");
        builder.HasKey(x => new { x.FirstUserId, x.SecondUserId });

        builder.Property(x => x.FirstUserId).IsRequired();
        builder.Property(x => x.SecondUserId).IsRequired();
        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<Conversation>.FromGuid(x))
            .IsRequired();

        builder.HasIndex(x => x.ConversationId);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

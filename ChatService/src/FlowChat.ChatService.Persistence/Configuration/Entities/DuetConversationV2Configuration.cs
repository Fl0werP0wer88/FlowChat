using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class DuetConversationV2Configuration
    : IEntityTypeConfiguration<DuetConversationLookupEntityV2>
{
    public void Configure(EntityTypeBuilder<DuetConversationLookupEntityV2> builder)
    {
        builder.ToTable("DuetConversationsV2");
        builder.HasKey(x => x.ConversationId);

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x));

        builder.Property(x => x.FirstUserId).IsRequired();
        builder.Property(x => x.SecondUserId).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => new { x.FirstUserId, x.SecondUserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_DuetConversationsV2_NormalizedUsers",
            "\"FirstUserId\" < \"SecondUserId\""));

        builder.HasOne<ConversationV2>()
            .WithOne()
            .HasForeignKey<DuetConversationLookupEntityV2>(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

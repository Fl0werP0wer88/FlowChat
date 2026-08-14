using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationMembershipReadEntityV2Configuration
    : IEntityTypeConfiguration<ConversationMembershipReadEntityV2>
{
    public void Configure(EntityTypeBuilder<ConversationMembershipReadEntityV2> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("ConversationMembershipsV2");

        builder.Property(x => x.Id);
        builder.Property(x => x.ConversationId);
        builder.Property(x => x.ConversationType);
        builder.Property(x => x.ParticipantCount);
        builder.Property(x => x.Version);
        builder.Property(x => x.DeletedAt);
    }
}

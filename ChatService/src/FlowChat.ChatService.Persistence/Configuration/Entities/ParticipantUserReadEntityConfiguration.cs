using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ParticipantUserReadEntityConfiguration : IEntityTypeConfiguration<ParticipantUserReadEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantUserReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("ParticipantUsers");

        builder.Property(x => x.Id);
        builder.Property(x => x.ConversationId);
        builder.Property(x => x.UserId);
        builder.Property(x => x.DisplayName);
        builder.Property(x => x.AvatarUrl);
        builder.Property(x => x.IsBlocked);
        builder.Property(x => x.IsMuted);
        builder.Property(x => x.IsHidden);
        builder.Property(x => x.LastReadMessageSequenceNum);
        builder.Property(x => x.DeletedAt);
    }
}

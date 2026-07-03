using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ParticipantUserConfiguration : IEntityTypeConfiguration<ParticipantUser>
{
    public void Configure(EntityTypeBuilder<ParticipantUser> builder)
    {
        builder.ToTable("ParticipantUsers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ParticipantUser>.FromGuid(x));

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<Conversation>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(x => x.Value, x => Id<UserProfileMarker>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(256);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.IsBlocked)
            .IsRequired();

        builder.Property(x => x.JoinedAtUtc)
            .HasUtcDateTimeOffsetConversion()
            .IsRequired();

        builder.Property(x => x.LastReadMessageSequenceNum)
            .IsRequired();

        builder.Property<DateTimeOffset?>("DeletedAt");

        builder.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        NpgsqlIndexBuilderExtensions.IncludeProperties(
                builder.HasIndex(x => new { x.UserId, x.ConversationId }),
                nameof(ParticipantUser.LastReadMessageSequenceNum))
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}

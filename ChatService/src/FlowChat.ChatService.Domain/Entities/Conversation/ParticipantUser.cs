using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ParticipantUser : EntityBase<ParticipantUser>
{
    public Id<Conversation> ConversationId { get; private set; }
    public Id<UserProfileMarker> UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsBlocked { get; private set; }
    public UtcDateTimeOffset JoinedAtUtc { get; private set; }

    private ParticipantUser(
        Id<ParticipantUser> id,
        Id<Conversation> conversationId,
        Id<UserProfileMarker> userId,
        string? displayName,
        string? avatarUrl,
        bool isBlocked,
        UtcDateTimeOffset joinedAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(userId);

        ConversationId = conversationId;
        UserId = userId;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        IsBlocked = isBlocked;
        JoinedAtUtc = joinedAtUtc;
    }

    public static ParticipantUser Create(
        Id<ParticipantUser> id,
        Id<Conversation> conversationId,
        Id<UserProfileMarker> userId,
        string? displayName = null,
        string? avatarUrl = null)
    {
        return new ParticipantUser(
            id,
            conversationId,
            userId,
            displayName,
            avatarUrl,
            isBlocked: false,
            UtcDateTimeOffset.UtcNow);
    }

    public static ParticipantUser Restore(
        Id<ParticipantUser> id,
        Id<Conversation> conversationId,
        Id<UserProfileMarker> userId,
        string? displayName,
        string? avatarUrl,
        bool isBlocked,
        UtcDateTimeOffset joinedAtUtc)
    {
        return new ParticipantUser(id, conversationId, userId, displayName, avatarUrl, isBlocked, joinedAtUtc);
    }

    internal void Block()
    {
        if (IsBlocked)
            throw new InvalidOperationException("Participant is already blocked.");

        IsBlocked = true;
    }

    internal void Unblock()
    {
        if (!IsBlocked)
            throw new InvalidOperationException("Participant is not blocked.");

        IsBlocked = false;
    }
}

using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ParticipantUser : EntityBase<ParticipantUser>
{
    public Id<Conversation> ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsBlocked { get; private set; }
    public UtcDateTimeOffset JoinedAtUtc { get; private set; }

    private ParticipantUser(
        Id<ParticipantUser> id,
        Id<Conversation> conversationId,
        Guid userId,
        string? displayName,
        string? avatarUrl,
        bool isBlocked,
        UtcDateTimeOffset joinedAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

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
        Guid userId,
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
        Guid userId,
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

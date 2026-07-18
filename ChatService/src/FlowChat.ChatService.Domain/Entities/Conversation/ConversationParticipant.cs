using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ConversationParticipant : AggregateRootBase<ConversationParticipant>
{
    public Id<ConversationAggregate> ConversationId { get; private set; }
    public Id<UserProfileMarker> UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool IsMuted { get; private set; }
    public bool IsHidden { get; private set; }
    public UtcDateTimeOffset JoinedAtUtc { get; private set; }
    public long LastReadMessageSequenceNum { get; private set; }

    private ConversationParticipant(
        Id<ConversationParticipant> id,
        Id<ConversationAggregate> conversationId,
        Id<UserProfileMarker> userId,
        string? displayName,
        bool isBlocked,
        bool isMuted,
        bool isHidden,
        UtcDateTimeOffset joinedAtUtc,
        long lastReadMessageSequenceNum) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(joinedAtUtc);

        if (lastReadMessageSequenceNum < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastReadMessageSequenceNum),
                "Last read message sequence number cannot be negative.");
        }

        ConversationId = conversationId;
        UserId = userId;
        DisplayName = NormalizeDisplayName(displayName);
        IsBlocked = isBlocked;
        IsMuted = isMuted;
        IsHidden = isHidden;
        JoinedAtUtc = joinedAtUtc;
        LastReadMessageSequenceNum = lastReadMessageSequenceNum;
    }

    public static ConversationParticipant Create(
        Id<ConversationParticipant> id,
        Id<ConversationAggregate> conversationId,
        Id<UserProfileMarker> userId,
        string? displayName = null,
        long lastReadMessageSequenceNum = 0,
        UtcDateTimeOffset? joinedAtUtc = null)
    {
        return new ConversationParticipant(
            id,
            conversationId,
            userId,
            displayName,
            isBlocked: false,
            isMuted: false,
            isHidden: false,
            joinedAtUtc ?? UtcDateTimeOffset.UtcNow,
            lastReadMessageSequenceNum);
    }

    public static ConversationParticipant Restore(
        Id<ConversationParticipant> id,
        Id<ConversationAggregate> conversationId,
        Id<UserProfileMarker> userId,
        string? displayName,
        bool isBlocked,
        bool isMuted,
        bool isHidden,
        UtcDateTimeOffset joinedAtUtc,
        long lastReadMessageSequenceNum)
    {
        return new ConversationParticipant(
            id,
            conversationId,
            userId,
            displayName,
            isBlocked,
            isMuted,
            isHidden,
            joinedAtUtc,
            lastReadMessageSequenceNum);
    }

    public bool ChangeDisplayName(string? displayName)
    {
        var normalizedDisplayName = NormalizeDisplayName(displayName);
        if (DisplayName == normalizedDisplayName)
        {
            return false;
        }

        DisplayName = normalizedDisplayName;
        return true;
    }

    public void Block()
    {
        if (IsBlocked)
        {
            throw new InvalidOperationException("Participant is already blocked.");
        }

        IsBlocked = true;
    }

    public void Unblock()
    {
        if (!IsBlocked)
        {
            throw new InvalidOperationException("Participant is not blocked.");
        }

        IsBlocked = false;
    }

    public bool Mute() => SetMuted(true);

    public bool Unmute() => SetMuted(false);

    public bool Hide() => SetHidden(true);

    public bool Unhide() => SetHidden(false);

    public bool MarkAsRead(long sequenceNum)
    {
        if (sequenceNum < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNum),
                "Last read message sequence number cannot be negative.");
        }

        if (sequenceNum <= LastReadMessageSequenceNum)
        {
            return false;
        }

        LastReadMessageSequenceNum = sequenceNum;
        return true;
    }

    private bool SetMuted(bool muted)
    {
        if (IsMuted == muted)
        {
            return false;
        }

        IsMuted = muted;
        return true;
    }

    private bool SetHidden(bool hidden)
    {
        if (IsHidden == hidden)
        {
            return false;
        }

        IsHidden = hidden;
        return true;
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? null
            : displayName.Trim();
    }
}

using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public abstract class Conversation : AggregateRootBase<Conversation>
{
    public ConversationType Type { get; private set; }
    public string? Name { get; private set; }
    public Id<UserProfileMarker> CreatedByUserId { get; private set; }
    public int MembershipRevision { get; protected set; } = 1;

    protected readonly List<ParticipantUser> _participants = [];
    public IReadOnlyCollection<ParticipantUser> Participants => _participants.AsReadOnly();

    // Required by EF Core: scalar-only constructor so EF can bind properties without the navigation collection.
    protected Conversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId) : base(id)
    {
        Type = type;
        Name = name?.Trim();
        CreatedByUserId = createdByUserId;
    }

    protected Conversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        List<ParticipantUser> participants) : this(id, type, name, createdByUserId)
    {
        _participants = participants;
    }

    protected static TConversation CreateCore<TConversation>(
        Id<Conversation> id,
        ConversationType type,
        Id<UserProfileMarker> createdByUserId,
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string? name,
        Func<Id<Conversation>, ConversationType, string?, Id<UserProfileMarker>, List<ParticipantUser>, TConversation> factory)
        where TConversation : Conversation
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateInvariants(type, name, createdByUserId);

        var conversationId = id;
        var participants = BuildParticipants(participantUserIds, type, conversationId);

        return factory(conversationId, type, name, createdByUserId, participants);
    }

    protected static TConversation RestoreCore<TConversation>(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        IEnumerable<ParticipantUser> participants,
        Func<Id<Conversation>, ConversationType, string?, Id<UserProfileMarker>, List<ParticipantUser>, TConversation> factory)
        where TConversation : Conversation
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory(id, type, name, createdByUserId, [.. participants]);
    }

    public bool MarkParticipantAsRead(Id<UserProfileMarker> participantUserId, long sequenceNum)
    {
        ArgumentNullException.ThrowIfNull(participantUserId);

        var participant = _participants.FirstOrDefault(p => p.UserId == participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        if (sequenceNum <= participant.LastReadMessageSequenceNum)
            return false;

        return participant.SetLastReadMessageSequenceNum(sequenceNum);
    }

    public bool HasParticipant(Id<UserProfileMarker> participantUserId)
    {
        ArgumentNullException.ThrowIfNull(participantUserId);

        return _participants.Any(p => p.UserId == participantUserId);
    }

    public ParticipantUser? GetParticipant(Id<UserProfileMarker> participantUserId)
    {
        ArgumentNullException.ThrowIfNull(participantUserId);

        return _participants.FirstOrDefault(p => p.UserId == participantUserId);
    }

    public bool UnhideParticipant(Id<UserProfileMarker> participantUserId)
    {
        var participant = GetParticipant(participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        return participant.SetHidden(false);
    }

    public bool HideParticipant(Id<UserProfileMarker> participantUserId)
    {
        var participant = GetParticipant(participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        return participant.SetHidden(true);
    }

    public bool MuteParticipant(Id<UserProfileMarker> participantUserId)
    {
        var participant = GetParticipant(participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        return participant.SetMuted(true);
    }

    public bool UnmuteParticipant(Id<UserProfileMarker> participantUserId)
    {
        var participant = GetParticipant(participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        return participant.SetMuted(false);
    }

    public void BlockParticipant(Id<UserProfileMarker> participantUserId)
    {
        var participant = GetParticipant(participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        participant.Block();
    }

    public void UnblockParticipant(Id<UserProfileMarker> participantUserId)
    {
        var participant = GetParticipant(participantUserId)
            ?? throw new InvalidOperationException("User is not a participant in this conversation.");

        participant.Unblock();
    }

    private static void ValidateInvariants(ConversationType type, string? name, Id<UserProfileMarker> createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(createdByUserId);

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Conversation type is invalid.", nameof(type));

        if (type == ConversationType.Group && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group conversations must have a name.", nameof(name));
    }

    private static List<ParticipantUser> BuildParticipants(
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        ConversationType type,
        Id<Conversation> conversationId)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var uniqueIds = participantUserIds
            .Where(id => id is not null)
            .Distinct()
            .ToList();

        if (type == ConversationType.Duet && uniqueIds.Count != 2)
            throw new InvalidOperationException("One-on-one conversations must have exactly two participants.");

        if (type == ConversationType.Group && uniqueIds.Count < 2)
            throw new InvalidOperationException("Group conversations must have at least two participants.");

        return [.. uniqueIds.Select(userId => ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, userId))];
    }
}

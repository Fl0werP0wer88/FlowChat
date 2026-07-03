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
    public long LastMsgSequenceNum { get; private set; }

    private readonly List<ParticipantUser> _participants = [];
    public IReadOnlyCollection<ParticipantUser> Participants => _participants.AsReadOnly();

    // Required by EF Core: scalar-only constructor so EF can bind properties without the navigation collection.
    protected Conversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum) : base(id)
    {
        Type = type;
        Name = name?.Trim();
        CreatedByUserId = createdByUserId;
        LastMsgSequenceNum = lastMsgSequenceNum;
    }

    protected Conversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum,
        List<ParticipantUser> participants) : this(id, type, name, createdByUserId, lastMsgSequenceNum)
    {
        _participants = participants;
    }

    protected static TConversation CreateCore<TConversation>(
        Id<Conversation> id,
        ConversationType type,
        Id<UserProfileMarker> createdByUserId,
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string? name,
        Func<Id<Conversation>, ConversationType, string?, Id<UserProfileMarker>, long, List<ParticipantUser>, TConversation> factory)
        where TConversation : Conversation
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateInvariants(type, name, createdByUserId);

        var conversationId = id;
        var participants = BuildParticipants(participantUserIds, type, conversationId);

        return factory(conversationId, type, name, createdByUserId, 0, participants);
    }

    protected static TConversation RestoreCore<TConversation>(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum,
        IEnumerable<ParticipantUser> participants,
        Func<Id<Conversation>, ConversationType, string?, Id<UserProfileMarker>, long, List<ParticipantUser>, TConversation> factory)
        where TConversation : Conversation
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory(id, type, name, createdByUserId, lastMsgSequenceNum, [.. participants]);
    }

    public void SetSequenceNumber(long sequenceNum)
    {
        if (sequenceNum < LastMsgSequenceNum)
            throw new ArgumentException("Sequence number must be equal to or greater than the current last message sequence number.", nameof(sequenceNum));

        LastMsgSequenceNum = sequenceNum;
    }

    protected void AddParticipantCore(
        Id<UserProfileMarker> participantUserId,
        string? displayName,
        string? avatarUrl,
        long lastReadMessageSequenceNum)
    {
        if (Type != ConversationType.Group)
            throw new InvalidOperationException("Cannot add participants to a one-on-one conversation.");

        ArgumentNullException.ThrowIfNull(participantUserId);

        if (_participants.Any(p => p.UserId == participantUserId))
            throw new InvalidOperationException("User is already a participant in this conversation.");

        _participants.Add(ParticipantUser.Create(
            Id<ParticipantUser>.New(),
            Id,
            participantUserId,
            displayName,
            avatarUrl,
            lastReadMessageSequenceNum));
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

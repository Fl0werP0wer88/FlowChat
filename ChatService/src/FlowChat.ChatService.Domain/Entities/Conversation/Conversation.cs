using FlowChat.ChatService.Domain.Entities.Conversation.Constants;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public abstract class Conversation : AggregateRootBase<Conversation>
{
    public ConversationType Type { get; private set; }
    public string? Name { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<ParticipantUser> _participants = [];
    public IReadOnlyCollection<ParticipantUser> Participants => _participants.AsReadOnly();

    // Required by EF Core — scalar-only constructor so EF can bind properties without the navigation collection
    protected Conversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId) : base(id)
    {
        Type = type;
        Name = name?.Trim();
        CreatedByUserId = createdByUserId;
    }

    protected Conversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId,
        List<ParticipantUser> participants) : this(id, type, name, createdByUserId)
    {
        _participants = participants;
    }

    protected static TConversation CreateCore<TConversation>(
        Id<Conversation> id,
        ConversationType type,
        Guid createdByUserId,
        IEnumerable<Guid> participantUserIds,
        string? name,
        Func<Id<Conversation>, ConversationType, string?, Guid, List<ParticipantUser>, TConversation> factory)
        where TConversation : Conversation
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateInvariants(type, name, createdByUserId);

        var conversationId = id;
        var participants = BuildParticipants(participantUserIds, type, conversationId);

        var conversation = factory(conversationId, type, name, createdByUserId, participants);

        conversation.AddDomainEvent(new ConversationCreatedDomainEvent(
            conversation.Id,
            conversation.Type,
            conversation.Name,
            conversation.CreatedByUserId,
            [ .. conversation.Participants.Select(p => p.UserId)]));

        conversation.MarkAggregateStateChanged(
            ConversationConstants.ConversationAggregateTypeName,
            () => new ConversationSnapshot(
                conversation.Id,
                conversation.Type,
                conversation.Name,
                conversation.CreatedByUserId,
                [ .. conversation.Participants.Select(p => p.UserId)]));

        return conversation;
    }

    protected static TConversation RestoreCore<TConversation>(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId,
        IEnumerable<ParticipantUser> participants,
        Func<Id<Conversation>, ConversationType, string?, Guid, List<ParticipantUser>, TConversation> factory)
        where TConversation : Conversation
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory(id, type, name, createdByUserId, [.. participants]);
    }

    protected void AddParticipantCore(Guid participantUserId, string? displayedName, string? avatarUrl)
    {
        if (Type != ConversationType.Group)
            throw new InvalidOperationException("Cannot add participants to a one-on-one conversation.");

        if (participantUserId == Guid.Empty)
            throw new ArgumentException("ParticipantUserId is required.", nameof(participantUserId));

        if (_participants.Any(p => p.UserId == participantUserId))
            throw new InvalidOperationException("User is already a participant in this conversation.");

        _participants.Add(ParticipantUser.Create(Id<ParticipantUser>.New(), Id, participantUserId, displayedName, avatarUrl));

        AddDomainEvent(new ParticipantAddedDomainEvent(Id, participantUserId));

        MarkAggregateStateChanged(
            ConversationConstants.ConversationAggregateTypeName,
            () => new ConversationSnapshot(
                Id,
                Type,
                Name,
                CreatedByUserId,
                [ .. Participants.Select(p => p.UserId)]));
    }

    private static void ValidateInvariants(ConversationType type, string? name, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Conversation type is invalid.", nameof(type));

        if (type == ConversationType.Group && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group conversations must have a name.", nameof(name));
    }

    private static List<ParticipantUser> BuildParticipants(
        IEnumerable<Guid> participantUserIds,
        ConversationType type,
        Id<Conversation> conversationId)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var uniqueIds = participantUserIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (type == ConversationType.Duet && uniqueIds.Count != 2)
            throw new InvalidOperationException("One-on-one conversations must have exactly two participants.");

        if (type == ConversationType.Group && uniqueIds.Count < 2)
            throw new InvalidOperationException("Group conversations must have at least two participants.");

        return [.. uniqueIds.Select(userId => ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, userId))];
    }
}

public record ConversationSnapshot(
    Id<Conversation> Id,
    ConversationType Type,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

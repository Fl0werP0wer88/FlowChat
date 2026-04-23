using FlowChat.ChatService.Domain.Entities.Conversation.Constants;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class Conversation : AggregateRootBase<Conversation>
{
    public bool IsGroup { get; private set; }
    public string? Name { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<ParticipantUser> _participants = [];
    public IReadOnlyCollection<ParticipantUser> Participants => _participants.AsReadOnly();

    // Required by EF Core — scalar-only constructor so EF can bind properties without the navigation collection
    private Conversation(
        Id<Conversation> id,
        bool isGroup,
        string? name,
        Guid createdByUserId) : base(id)
    {
        IsGroup = isGroup;
        Name = name?.Trim();
        CreatedByUserId = createdByUserId;
    }

    private Conversation(
        Id<Conversation> id,
        bool isGroup,
        string? name,
        Guid createdByUserId,
        List<ParticipantUser> participants) : this(id, isGroup, name, createdByUserId)
    {
        _participants = participants;
    }

    public static Conversation Create(
        Id<Conversation> id,
        bool isGroup,
        Guid createdByUserId,
        IEnumerable<Guid> participantUserIds,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ValidateInvariants(isGroup, name, createdByUserId);

        var conversationId = id;
        var participants = BuildParticipants(participantUserIds, isGroup, conversationId);

        var conversation = new Conversation(conversationId, isGroup, name, createdByUserId, participants);

        conversation.AddDomainEvent(new ConversationCreatedDomainEvent(
            conversation.Id,
            conversation.IsGroup,
            conversation.Name,
            conversation.CreatedByUserId,
            [ .. conversation.Participants.Select(p => p.UserId)]));

        conversation.MarkAggregateStateChanged(
            ConversationConstants.ConversationAggregateTypeName,
            () => new ConversationSnapshot(
                conversation.Id,
                conversation.IsGroup,
                conversation.Name,
                conversation.CreatedByUserId,
                [ .. conversation.Participants.Select(p => p.UserId)]));

        return conversation;
    }

    public static Conversation Restore(
        Id<Conversation> id,
        bool isGroup,
        string? name,
        Guid createdByUserId,
        IEnumerable<ParticipantUser> participants)
    {
        return new Conversation(id, isGroup, name, createdByUserId, [.. participants]);
    }

    public void AddParticipant(Guid participantUserId)
    {
        if (!IsGroup)
            throw new InvalidOperationException("Cannot add participants to a one-on-one conversation.");

        if (participantUserId == Guid.Empty)
            throw new ArgumentException("ParticipantUserId is required.", nameof(participantUserId));

        if (_participants.Any(p => p.UserId == participantUserId))
            throw new InvalidOperationException("User is already a participant in this conversation.");

        _participants.Add(ParticipantUser.Create(Id<ParticipantUser>.New(), Id, participantUserId));

        AddDomainEvent(new ParticipantAddedDomainEvent(Id, participantUserId));

        MarkAggregateStateChanged(
            ConversationConstants.ConversationAggregateTypeName,
            () => new ConversationSnapshot(
                Id,
                IsGroup,
                Name,
                CreatedByUserId,
                [ .. Participants.Select(p => p.UserId)]));
    }

    private static void ValidateInvariants(bool isGroup, string? name, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        if (isGroup && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group conversations must have a name.", nameof(name));
    }

    private static List<ParticipantUser> BuildParticipants(
        IEnumerable<Guid> participantUserIds,
        bool isGroup,
        Id<Conversation> conversationId)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var uniqueIds = participantUserIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (!isGroup && uniqueIds.Count != 2)
            throw new InvalidOperationException("One-on-one conversations must have exactly two participants.");

        if (isGroup && uniqueIds.Count < 2)
            throw new InvalidOperationException("Group conversations must have at least two participants.");

        return [.. uniqueIds.Select(userId => ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, userId))];
    }
}

public record ConversationSnapshot(
    Id<Conversation> Id,
    bool IsGroup,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

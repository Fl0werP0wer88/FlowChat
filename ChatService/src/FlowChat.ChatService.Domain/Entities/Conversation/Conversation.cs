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

    private readonly List<Guid> _participantUserIds = [];
    public IReadOnlyCollection<Guid> ParticipantUserIds => _participantUserIds.AsReadOnly();

    private Conversation(
        Id<Conversation>? id,
        bool isGroup,
        string? name,
        Guid createdByUserId,
        IEnumerable<Guid> participantUserIds) : base(id)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        if (isGroup && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group conversations must have a name.", nameof(name));

        IsGroup = isGroup;
        Name = name?.Trim();
        CreatedByUserId = createdByUserId;
        _participantUserIds = NormalizeParticipants(participantUserIds, isGroup);
    }

    public static Conversation Create(
        bool isGroup,
        Guid createdByUserId,
        IEnumerable<Guid> participantUserIds,
        string? name = null,
        Id<Conversation>? id = null)
    {
        var conversation = new Conversation(
            id ?? Id<Conversation>.New(),
            isGroup,
            name,
            createdByUserId,
            participantUserIds);

        conversation.AddDomainEvent(new ConversationCreatedDomainEvent(
            conversation.Id,
            conversation.IsGroup,
            conversation.Name,
            conversation.CreatedByUserId,
            conversation.ParticipantUserIds));

        conversation.MarkAggregateStateChanged(
            ConversationConstants.ConversationAggregateTypeName,
            () => new ConversationSnapshot(
                conversation.Id,
                conversation.IsGroup,
                conversation.Name,
                conversation.CreatedByUserId,
                conversation.ParticipantUserIds));

        return conversation;
    }

    public void AddParticipant(Guid participantUserId)
    {
        if (!IsGroup)
            throw new InvalidOperationException("Cannot add participants to a one-on-one conversation.");

        if (participantUserId == Guid.Empty)
            throw new ArgumentException("ParticipantUserId is required.", nameof(participantUserId));

        if (_participantUserIds.Contains(participantUserId))
            throw new InvalidOperationException("User is already a participant in this conversation.");

        _participantUserIds.Add(participantUserId);

        AddDomainEvent(new ParticipantAddedDomainEvent(Id, participantUserId));

        MarkAggregateStateChanged(
            ConversationConstants.ConversationAggregateTypeName,
            () => new ConversationSnapshot(Id, IsGroup, Name, CreatedByUserId, ParticipantUserIds));
    }

    public static Conversation Restore(
        Id<Conversation> id,
        bool isGroup,
        string? name,
        Guid createdByUserId,
        IEnumerable<Guid> participantUserIds)
    {
        return new Conversation(id, isGroup, name, createdByUserId, participantUserIds);
    }

    private static List<Guid> NormalizeParticipants(IEnumerable<Guid> participantUserIds, bool isGroup)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var participants = participantUserIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (!isGroup && participants.Count != 2)
            throw new InvalidOperationException("One-on-one conversations must have exactly two participants.");

        if (isGroup && participants.Count < 2)
            throw new InvalidOperationException("Group conversations must have at least two participants.");

        return participants;
    }
}

public record ConversationSnapshot(
    Id<Conversation> Id,
    bool IsGroup,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ConversationV2 : AggregateRootBase<ConversationV2>
{
    private const int DuetParticipantsCount = 2;
    private const int MinimumGroupParticipantsCount = 2;

    public ConversationType ConversationType { get; private set; }
    public string? Name { get; private set; }
    public Id<UserProfileMarker> CreatedByUserId { get; private set; }

    private ConversationV2(
        Id<ConversationV2> id,
        ConversationType conversationType,
        string? name,
        Id<UserProfileMarker> createdByUserId) : base(id)
    {
        ArgumentNullException.ThrowIfNull(createdByUserId);

        ValidateConversationType(conversationType);
        ValidateName(conversationType, name);

        ConversationType = conversationType;
        Name = NormalizeName(name);
        CreatedByUserId = createdByUserId;
    }

    public static ConversationV2 CreateGroup(
        Id<ConversationV2> id,
        Id<UserProfileMarker> createdByUserId,
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string name)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var normalizedParticipantUserIds = NormalizeParticipantUserIds(
            participantUserIds.Prepend(createdByUserId));

        if (normalizedParticipantUserIds.Count < MinimumGroupParticipantsCount)
        {
            throw new InvalidOperationException(
                "Group conversations must have at least two participants.");
        }

        return Create(
            id,
            ConversationType.Group,
            name,
            createdByUserId,
            normalizedParticipantUserIds);
    }

    public static ConversationV2 CreateDuet(
        Id<ConversationV2> id,
        Id<UserProfileMarker> createdByUserId,
        Id<UserProfileMarker> partnerUserId)
    {
        ArgumentNullException.ThrowIfNull(partnerUserId);

        var normalizedParticipantUserIds = NormalizeParticipantUserIds(
            [createdByUserId, partnerUserId]);

        if (normalizedParticipantUserIds.Count != DuetParticipantsCount)
        {
            throw new InvalidOperationException(
                "One-on-one conversations must have exactly two distinct participants.");
        }

        return Create(
            id,
            ConversationType.Duet,
            name: null,
            createdByUserId,
            normalizedParticipantUserIds);
    }

    public static ConversationV2 CreateDuet(
        Id<UserProfileMarker> createdByUserId,
        Id<UserProfileMarker> partnerUserId)
    {
        return CreateDuet(
            Id<ConversationV2>.New(),
            createdByUserId,
            partnerUserId);
    }

    public static ConversationV2 Restore(
        Id<ConversationV2> id,
        ConversationType conversationType,
        string? name,
        Id<UserProfileMarker> createdByUserId)
    {
        return new ConversationV2(
            id,
            conversationType,
            name,
            createdByUserId);
    }

    private static ConversationV2 Create(
        Id<ConversationV2> id,
        ConversationType conversationType,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        IReadOnlyCollection<Id<UserProfileMarker>> participantUserIds)
    {
        var conversation = new ConversationV2(
            id,
            conversationType,
            name,
            createdByUserId);

        conversation.AddDomainEvent(
            new ConversationCreatedDomainEventV2(
                conversation.Id,
                conversation.ConversationType,
                conversation.Name,
                conversation.CreatedByUserId,
                participantUserIds));

        return conversation;
    }

    private static IReadOnlyCollection<Id<UserProfileMarker>> NormalizeParticipantUserIds(
        IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        return participantUserIds
            .Where(userId => userId is not null)
            .Distinct()
            .ToArray();
    }

    private static void ValidateConversationType(ConversationType conversationType)
    {
        if (!Enum.IsDefined(conversationType))
        {
            throw new ArgumentException(
                "Conversation type is invalid.",
                nameof(conversationType));
        }
    }

    private static void ValidateName(ConversationType conversationType, string? name)
    {
        if (conversationType == ConversationType.Group &&
            string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Group conversations must have a name.",
                nameof(name));
        }

        if (conversationType == ConversationType.Duet &&
            !string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "One-on-one conversations cannot have a name.",
                nameof(name));
        }
    }

    private static string? NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim();
    }
}

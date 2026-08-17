using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.Conversation.ValueObjects;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ConversationV2 : AggregateRootBase<ConversationV2>
{
    private const int MinimumGroupParticipantsCount = 2;

    public ConversationType ConversationType { get; private set; }
    public string? Name { get; private set; }
    public DuetParticipantPair? DuetParticipants { get; private set; }

    private ConversationV2(Id<ConversationV2> id) : base(id)
    {
    }

    private ConversationV2(
        Id<ConversationV2> id,
        ConversationType conversationType,
        string? name,
        DuetParticipantPair? duetParticipants) : base(id)
    {
        ValidateConversationType(conversationType);
        ValidateName(conversationType, name);
        ValidateDuetParticipants(conversationType, duetParticipants);

        ConversationType = conversationType;
        Name = NormalizeName(name);
        DuetParticipants = duetParticipants;
    }

    public static ConversationV2 CreateGroup(
        Id<ConversationV2> id,
        Id<UserProfileMarker> requestingUserId,
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string name)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var normalizedParticipantUserIds = NormalizeParticipantUserIds(
            participantUserIds.Prepend(requestingUserId));

        if (normalizedParticipantUserIds.Count < MinimumGroupParticipantsCount)
        {
            throw new InvalidOperationException(
                "Group conversations must have at least two participants.");
        }

        return Create(
            id,
            ConversationType.Group,
            name,
            duetParticipants: null,
            normalizedParticipantUserIds);
    }

    public static ConversationV2 CreateDuet(
        Id<ConversationV2> id,
        Id<UserProfileMarker> requestingUserId,
        Id<UserProfileMarker> partnerUserId)
    {
        ArgumentNullException.ThrowIfNull(partnerUserId);

        var duetParticipants = DuetParticipantPair.Create(requestingUserId, partnerUserId);
        var normalizedParticipantUserIds = new[]
        {
            duetParticipants.FirstUserId,
            duetParticipants.SecondUserId
        };

        return Create(
            id,
            ConversationType.Duet,
            name: null,
            duetParticipants,
            normalizedParticipantUserIds);
    }

    public static ConversationV2 CreateDuet(
        Id<UserProfileMarker> requestingUserId,
        Id<UserProfileMarker> partnerUserId)
    {
        return CreateDuet(
            Id<ConversationV2>.New(),
            requestingUserId,
            partnerUserId);
    }

    public static ConversationV2 Restore(
        Id<ConversationV2> id,
        ConversationType conversationType,
        string? name,
        DuetParticipantPair? duetParticipants)
    {
        return new ConversationV2(
            id,
            conversationType,
            name,
            duetParticipants);
    }

    private static ConversationV2 Create(
        Id<ConversationV2> id,
        ConversationType conversationType,
        string? name,
        DuetParticipantPair? duetParticipants,
        IReadOnlyCollection<Id<UserProfileMarker>> participantUserIds)
    {
        var conversation = new ConversationV2(
            id,
            conversationType,
            name,
            duetParticipants);

        conversation.AddDomainEvent(
            new ConversationCreatedDomainEventV2(
                conversation.Id,
                conversation.ConversationType,
                conversation.Name,
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

    private static void ValidateDuetParticipants(
        ConversationType conversationType,
        DuetParticipantPair? duetParticipants)
    {
        if (conversationType == ConversationType.Duet && duetParticipants is null)
        {
            throw new ArgumentException(
                "One-on-one conversations must define their participants.",
                nameof(duetParticipants));
        }

        if (conversationType == ConversationType.Group && duetParticipants is not null)
        {
            throw new ArgumentException(
                "Group conversations cannot define duet participants.",
                nameof(duetParticipants));
        }
    }

    private static string? NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim();
    }
}

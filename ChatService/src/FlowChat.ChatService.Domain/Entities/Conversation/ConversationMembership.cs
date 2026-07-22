using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ConversationMembership : AggregateRootBase<ConversationMembership>
{
    private const int DuetParticipantsCount = 2;
    private const int MinimumGroupParticipantsCount = 2;

    public Id<ConversationV2> ConversationId { get; private set; }
    public ConversationType ConversationType { get; private set; }
    public int ParticipantCount { get; private set; }

    private ConversationMembership(
        Id<ConversationMembership> id,
        Id<ConversationV2> conversationId,
        ConversationType conversationType,
        int participantCount) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);

        if (id.Value != conversationId.Value)
        {
            throw new ArgumentException(
                "Conversation membership id must match the conversation id.",
                nameof(id));
        }

        ValidateParticipantCount(conversationType, participantCount);

        ConversationId = conversationId;
        ConversationType = conversationType;
        ParticipantCount = participantCount;
    }

    public static ConversationMembership Create(
        Id<ConversationV2> conversationId,
        ConversationType conversationType,
        IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        var normalizedParticipantUserIds = NormalizeParticipantUserIds(participantUserIds);

        var membership = new ConversationMembership(
            Id<ConversationMembership>.FromId(conversationId),
            conversationId,
            conversationType,
            normalizedParticipantUserIds.Count);

        membership.AddParticipantsAddedEvent(
            normalizedParticipantUserIds,
            initialReadCursor: 0);

        return membership;
    }

    public static ConversationMembership Restore(
        Id<ConversationMembership> id,
        Id<ConversationV2> conversationId,
        ConversationType conversationType,
        int participantCount)
    {
        return new ConversationMembership(
            id,
            conversationId,
            conversationType,
            participantCount);
    }

    public void AddParticipants(
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        long initialReadCursor)
    {
        var normalizedParticipantUserIds = NormalizeParticipantUserIds(participantUserIds);
        ValidateInitialReadCursor(initialReadCursor);

        var newParticipantCount = checked(ParticipantCount + normalizedParticipantUserIds.Count);
        ValidateParticipantCount(ConversationType, newParticipantCount);

        ParticipantCount = newParticipantCount;
        AddParticipantsAddedEvent(
            normalizedParticipantUserIds,
            initialReadCursor);
    }

    public void RemoveParticipants(IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        var normalizedParticipantUserIds = NormalizeParticipantUserIds(participantUserIds);

        if (normalizedParticipantUserIds.Count > ParticipantCount)
        {
            throw new InvalidOperationException(
                "Cannot remove more participants than the conversation currently has.");
        }

        var newParticipantCount = ParticipantCount - normalizedParticipantUserIds.Count;
        ValidateParticipantCount(ConversationType, newParticipantCount);

        ParticipantCount = newParticipantCount;
        AddParticipantsRemovedEvent(normalizedParticipantUserIds);
    }

    private void AddParticipantsAddedEvent(
        IReadOnlyList<Id<UserProfileMarker>> participantUserIds,
        long initialReadCursor)
    {
        AddDomainEvent(new ConversationParticipantsAddedDomainEventV2(
            Id,
            ConversationId,
            participantUserIds,
            initialReadCursor));
    }

    private void AddParticipantsRemovedEvent(
        IReadOnlyList<Id<UserProfileMarker>> participantUserIds)
    {
        AddDomainEvent(new ConversationParticipantsRemovedDomainEventV2(
            Id,
            ConversationId,
            participantUserIds));
    }

    private static void ValidateInitialReadCursor(long initialReadCursor)
    {
        if (initialReadCursor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialReadCursor),
                "Initial read cursor cannot be negative.");
        }
    }

    private static IReadOnlyList<Id<UserProfileMarker>> NormalizeParticipantUserIds(
        IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var userIds = participantUserIds.ToList();

        if (userIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one participant must be provided.",
                nameof(participantUserIds));
        }

        if (userIds.Any(userId => userId is null))
        {
            throw new ArgumentException(
                "Participant user id cannot be null.",
                nameof(participantUserIds));
        }

        if (userIds.Distinct().Count() != userIds.Count)
        {
            throw new ArgumentException(
                "Participant user ids cannot contain duplicates.",
                nameof(participantUserIds));
        }

        return userIds;
    }

    private static void ValidateParticipantCount(
        ConversationType conversationType,
        int participantCount)
    {
        if (!Enum.IsDefined(conversationType))
        {
            throw new ArgumentException(
                "Conversation type is invalid.",
                nameof(conversationType));
        }

        if (conversationType == ConversationType.Duet &&
            participantCount != DuetParticipantsCount)
        {
            throw new InvalidOperationException(
                "One-on-one conversations must have exactly two participants.");
        }

        if (conversationType == ConversationType.Group &&
            participantCount < MinimumGroupParticipantsCount)
        {
            throw new InvalidOperationException(
                "Group conversations must have at least two participants.");
        }
    }
}

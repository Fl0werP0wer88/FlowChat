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

        membership.AddMembershipChangedEvents(
            normalizedParticipantUserIds,
            ConversationMembershipDeltaOperation.Added);

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

    public void AddParticipants(IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        var normalizedParticipantUserIds = NormalizeParticipantUserIds(participantUserIds);

        var newParticipantCount = checked(ParticipantCount + normalizedParticipantUserIds.Count);
        ValidateParticipantCount(ConversationType, newParticipantCount);

        ParticipantCount = newParticipantCount;
        AddMembershipChangedEvents(
            normalizedParticipantUserIds,
            ConversationMembershipDeltaOperation.Added);
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
        AddMembershipChangedEvents(
            normalizedParticipantUserIds,
            ConversationMembershipDeltaOperation.Removed);
    }

    private void AddMembershipChangedEvents(
        IReadOnlyCollection<Id<UserProfileMarker>> participantUserIds,
        ConversationMembershipDeltaOperation operation)
    {

        //Review1: Pisaleś że będzie pobierał maksymalny SequenceNum wiadomości V2 dla konwersacji. To niedoprze bo pobieranie będzie się odbywalo dla kazdej pozycji z listy.
        //Review1: Może by tak pobierać maksymalny SequenceNum wiadomości V2 jeszcze w handlerze pobierającym  agregat ConversationMembership i przekazywać go do AddMembershipChangedEvents? Oceń Pomysł
        foreach (var participantUserId in participantUserIds)
        {
            AddDomainEvent(operation == ConversationMembershipDeltaOperation.Added
                ? new ConversationParticipantAddedDomainEventV2(
                    Id,
                    ConversationId,
                    participantUserId)
                : new ConversationParticipantRemovedDomainEventV2(
                    Id,
                    ConversationId,
                    participantUserId));
        }

        //Review1: Skoro delta integration event publikowac przez IAggregateBeforeSaveProcessor to ConversationMembershipDeltaDomainEventV2 Jest chyba do Wyrzucenia prawda?
        AddDomainEvent(new ConversationMembershipDeltaDomainEventV2(
            Id,
            ConversationId,
            operation,
            participantUserIds,
            ParticipantCount));
    }

    private static IReadOnlyCollection<Id<UserProfileMarker>> NormalizeParticipantUserIds(
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

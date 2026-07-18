using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class ConversationMembership : AggregateRootBase<ConversationMembership>
{
    private const int DuetParticipantsCount = 2;
    private const int MinimumGroupParticipantsCount = 2;

    public Id<ConversationAggregate> ConversationId { get; private set; }
    public ConversationType ConversationType { get; private set; }
    public int ParticipantCount { get; private set; }

    private ConversationMembership(
        Id<ConversationMembership> id,
        Id<ConversationAggregate> conversationId,
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
        Id<ConversationAggregate> conversationId,
        ConversationType conversationType,
        int participantCount)
    {
        ArgumentNullException.ThrowIfNull(conversationId);

        return new ConversationMembership(
            Id<ConversationMembership>.FromId(conversationId),
            conversationId,
            conversationType,
            participantCount);
    }

    public static ConversationMembership Restore(
        Id<ConversationMembership> id,
        Id<ConversationAggregate> conversationId,
        ConversationType conversationType,
        int participantCount)
    {
        return new ConversationMembership(
            id,
            conversationId,
            conversationType,
            participantCount);
    }

    public void AddParticipants(int participantCount)
    {
        EnsurePositiveParticipantCount(participantCount);

        var newParticipantCount = checked(ParticipantCount + participantCount);
        ValidateParticipantCount(ConversationType, newParticipantCount);

        ParticipantCount = newParticipantCount;
    }

    public void RemoveParticipants(int participantCount)
    {
        EnsurePositiveParticipantCount(participantCount);

        if (participantCount > ParticipantCount)
        {
            throw new InvalidOperationException(
                "Cannot remove more participants than the conversation currently has.");
        }

        var newParticipantCount = ParticipantCount - participantCount;
        ValidateParticipantCount(ConversationType, newParticipantCount);

        ParticipantCount = newParticipantCount;
    }

    private static void EnsurePositiveParticipantCount(int participantCount)
    {
        if (participantCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(participantCount),
                "Participant count must be greater than zero.");
        }
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

using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaProjectionMetadataProviderV2<TTrigger>
    : IAggregateDeltaProjectionMetadataProviderV2<TTrigger, ConversationParticipant>
    where TTrigger : class, IConversationParticipantsChangedDomainEventV2
{
    public Guid GetProjectionId(
        TTrigger notification,
        IReadOnlyList<AggregateDeltaMutation<ConversationParticipant>> mutations)
    {
        Validate(notification, mutations);
        return notification.ConversationId.Value;
    }

    public int GetProjectionRevision(
        TTrigger notification,
        IReadOnlyList<AggregateDeltaMutation<ConversationParticipant>> mutations)
    {
        Validate(notification, mutations);
        return notification.Version;
    }

    public string GetKafkaKey(
        TTrigger notification,
        IReadOnlyList<AggregateDeltaMutation<ConversationParticipant>> mutations)
    {
        Validate(notification, mutations);
        return notification.ConversationId.Value.ToString("D");
    }

    private static void Validate(
        TTrigger notification,
        IReadOnlyList<AggregateDeltaMutation<ConversationParticipant>> mutations)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(mutations);

        if (mutations.Count == 0)
        {
            throw new InvalidOperationException(
                "A conversation membership delta requires at least one participant mutation.");
        }

        var conversationId = notification.ConversationId;
        if (mutations.Any(mutation => mutation.Aggregate.ConversationId != conversationId))
        {
            throw new InvalidOperationException(
                "All conversation membership delta mutations must belong to the conversation from the notification.");
        }

    }
}

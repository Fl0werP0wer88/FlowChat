using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaProjectionKeyProviderV2<TTrigger>
    : IAggregateDeltaProjectionKeyProviderV2<TTrigger, ConversationParticipant>
    where TTrigger : class, IConversationParticipantsChangedDomainEventV2
{
    public string GetKafkaKey(
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

        return conversationId.Value.ToString("D");
    }
}

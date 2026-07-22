using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaProjectionKeyProviderV2<TTrigger>
    : IAggregateDeltaProjectionKeyProviderV2<TTrigger, ConversationParticipant>
{
    //Revew3-1: Na tym Etapie powinnismy juz znac jaki command to wywołał (Czyli powinno byc  ConversationMembershipDeltaProjectionKeyProviderV2<{Konkretna kklasa Commanda}>) i prawdopodobnie wlasnie z tamtąd brac ConversationId
    public string GetKafkaKey(
        TTrigger command,
        IReadOnlyList<AggregateDeltaMutation<ConversationParticipant>> mutations)
    {
        ArgumentNullException.ThrowIfNull(mutations);

        if (mutations.Count == 0)
        {
            throw new InvalidOperationException(
                "A conversation membership delta requires at least one participant mutation.");
        }

        var conversationId = mutations[0].Aggregate.ConversationId.Value;
        if (mutations.Any(x => x.Aggregate.ConversationId.Value != conversationId))
        {
            throw new InvalidOperationException(
                "All conversation membership delta mutations must belong to the same conversation.");
        }

        return conversationId.ToString("D");
    }
}

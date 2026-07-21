using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class UnhideConversationParticipantsOnChatMessageSentDomainEventHandlerV2(
    IConversationParticipantWriteRepository participantRepository,
    IEnumerable<IAggregateBeforeSaveProcessorV2<
        ChatMessageSentDomainEventV2,
        ConversationParticipant>> processors)
    : IDomainEventHandler<ChatMessageSentDomainEventV2>
{
    public async Task Handle(
        ChatMessageSentDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        var participants = await participantRepository.GetHiddenByConversationIdAsync(
            notification.ConversationId,
            notification.SenderUserId,
            cancellationToken);

        foreach (var participant in participants)
        {
            if (!participant.Unhide())
                continue;

            participant.IncrementVersion();
            participant.SetUpdated("system");

            foreach (var processor in processors)
            {
                await processor.ProcessAsync(
                    notification,
                    participant,
                    MutationType.Updated,
                    cancellationToken);
            }
        }
    }
}

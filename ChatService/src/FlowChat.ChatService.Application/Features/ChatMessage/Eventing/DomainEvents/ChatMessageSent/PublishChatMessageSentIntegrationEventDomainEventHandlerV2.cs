using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class PublishChatMessageSentIntegrationEventDomainEventHandlerV2(
    IConversationMembershipWriteRepository membershipRepository,
    IOutboxIntegrationEventPublisher publisher,
    IMapper mapper)
    : IDomainEventHandler<ChatMessageSentDomainEventV2>
{
    public async Task Handle(
        ChatMessageSentDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        var membershipVersion = await membershipRepository.GetVersionAsync(
            notification.ConversationId,
            cancellationToken);
        if (!membershipVersion.HasValue)
        {
            throw new InvalidOperationException(
                $"Conversation membership {notification.ConversationId} was not found.");
        }

        var integrationEvent = mapper.Map<ChatMessageSentIntegrationEventV2>(notification) with
        {
            ConversationMembershipRevision = membershipVersion.Value
        };

        await publisher.PublishAsync(
            integrationEvent,
            notification.ConversationId.Value.ToString("D"),
            cancellationToken);
    }
}

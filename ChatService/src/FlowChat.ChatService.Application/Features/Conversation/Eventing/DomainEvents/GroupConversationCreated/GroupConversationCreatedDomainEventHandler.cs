using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;

public sealed class GroupConversationCreatedDomainEventHandler
    : IDomainEventHandler<GroupConversationCreatedDomainEvent>
{
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IMapper _mapper;

    public GroupConversationCreatedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public Task Handle(
        GroupConversationCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = _mapper.Map<GroupConversationChangedIntegrationEvent>(notification);

        return _integrationEventPublisher.PublishAsync(
            integrationEvent,
            notification.ConversationId.ToString("D"),
            cancellationToken);
    }
}

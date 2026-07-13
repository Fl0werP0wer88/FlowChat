using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class ChatMessageSentDomainEventHandler
    : IDomainEventHandler<ChatMessageSentDomainEvent>
{
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IMapper _mapper;
    private readonly IConversationParticipantReadRepository _participantReadRepository;

    public ChatMessageSentDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper,
        IConversationParticipantReadRepository participantReadRepository)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _participantReadRepository = participantReadRepository
            ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    // Conversation's version is read here rather than threaded through ChatMessage.Create(...) because it belongs
    // to a foreign aggregate; the domain event/aggregate has no business knowing another aggregate's version.
    public async Task Handle(
        ChatMessageSentDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = _mapper.Map<ChatMessageSentIntegrationEvent>(notification);
        var conversationVersion = await _participantReadRepository.GetVersionAsync(
            notification.ConversationId.Value,
            cancellationToken);

        integrationEvent = integrationEvent with { ConversationVersionAtSend = conversationVersion ?? 0 };

        await _integrationEventPublisher.PublishAsync(
            integrationEvent,
            notification.ConversationId.ToString(),
            cancellationToken);
    }
}

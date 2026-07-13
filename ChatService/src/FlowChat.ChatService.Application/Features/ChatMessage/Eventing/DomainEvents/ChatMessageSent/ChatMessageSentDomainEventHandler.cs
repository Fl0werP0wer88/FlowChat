using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class ChatMessageSentDomainEventHandler
    : MappedDomainEventHandlerBase<ChatMessageSentDomainEvent, ChatMessageSentIntegrationEvent>
{
    private readonly IMapper _mapper;
    private readonly IConversationParticipantReadRepository _participantReadRepository;

    public ChatMessageSentDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper,
        IConversationParticipantReadRepository participantReadRepository)
        : base(integrationEventPublisher, mapper)
    {
        _mapper = mapper;
        _participantReadRepository = participantReadRepository
            ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    // Conversation's version is read here rather than threaded through ChatMessage.Create(...) because it belongs
    // to a foreign aggregate; the domain event/aggregate has no business knowing another aggregate's version.
    protected override async Task<ChatMessageSentIntegrationEvent> MapToIntegrationEvent(
        ChatMessageSentDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = _mapper.Map<ChatMessageSentIntegrationEvent>(notification);
        var conversationVersion = await _participantReadRepository.GetVersionAsync(
            notification.ConversationId.Value,
            cancellationToken);

        return integrationEvent with { ConversationVersionAtSend = conversationVersion ?? 0 };
    }

    protected override string ResolveKafkaKey(
        ChatMessageSentDomainEvent notification,
        ChatMessageSentIntegrationEvent integrationEvent) =>
        notification.ConversationId.ToString();
}

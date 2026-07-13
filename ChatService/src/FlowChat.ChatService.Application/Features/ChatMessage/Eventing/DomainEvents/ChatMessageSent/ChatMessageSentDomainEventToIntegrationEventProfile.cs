using AutoMapper;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class ChatMessageSentDomainEventToIntegrationEventProfile : Profile
{
    public ChatMessageSentDomainEventToIntegrationEventProfile()
    {
        CreateMap<ChatMessageSentDomainEvent, ChatMessageSentIntegrationEvent>()
            .ForMember(destination => destination.ConversationId, options => options.MapFrom(source => source.ConversationId.Value))
            .ForMember(destination => destination.SenderUserId, options => options.MapFrom(source => source.SenderUserId.Value))
            .ForMember(destination => destination.SentAtUtc, options => options.MapFrom(source => source.SentAtUtc.Value))
            // Stamped separately in ChatMessageSentDomainEventHandler from a fresh Conversation-version read, since it belongs to a foreign aggregate the domain event cannot know about.
            .ForMember(destination => destination.ConversationVersionAtSend, options => options.Ignore());
    }
}

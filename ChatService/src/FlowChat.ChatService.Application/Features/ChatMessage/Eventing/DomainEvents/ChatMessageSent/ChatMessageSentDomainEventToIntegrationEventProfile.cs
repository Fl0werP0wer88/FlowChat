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
            .ForMember(destination => destination.RecipientUserIds, options => options.MapFrom(source => source.RecipientUserIds.Select(id => id.Value).ToList()));
    }
}

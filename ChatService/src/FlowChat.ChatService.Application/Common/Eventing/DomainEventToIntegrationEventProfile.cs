using AutoMapper;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<ChatMessageSentDomainEvent, ChatMessageSentIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.ConversationId.ToString()))
            .ForMember(destination => destination.SentAtUtc, options => options.MapFrom(source => source.SentAtUtc.Value))
            .ForMember(destination => destination.RecipientUserIds, options => options.MapFrom(source => source.RecipientUserIds.ToList()));
    }
}

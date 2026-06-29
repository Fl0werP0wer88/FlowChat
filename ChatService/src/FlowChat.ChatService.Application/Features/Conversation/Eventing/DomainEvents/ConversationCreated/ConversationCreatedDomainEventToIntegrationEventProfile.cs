using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;

public sealed class ConversationCreatedDomainEventToIntegrationEventProfile : Profile
{
    public ConversationCreatedDomainEventToIntegrationEventProfile()
    {
        CreateMap<ConversationCreatedDomainEvent, ConversationChangedIntegrationEvent>()
            .ForMember(destination => destination.Type, options => options.MapFrom(source => (int) source.Type))
            .ForMember(destination => destination.CreatedByUserId, options => options.MapFrom(source => source.CreatedByUserId.Value))
            .ForMember(destination => destination.ParticipantUserIds, options => options.MapFrom(source => source.ParticipantUserIds.Select(id => id.Value).ToList()));
    }
}

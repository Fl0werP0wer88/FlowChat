using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;

public sealed class GroupConversationCreatedDomainEventToIntegrationEventProfile : Profile
{
    public GroupConversationCreatedDomainEventToIntegrationEventProfile()
    {
        CreateMap<GroupConversationCreatedDomainEvent, GroupConversationChangedIntegrationEvent>()
            .ForMember(destination => destination.Type, options => options.MapFrom(source => (int) source.Type))
            .ForMember(destination => destination.CreatedByUserId, options => options.MapFrom(source => source.CreatedByUserId.Value));
    }
}

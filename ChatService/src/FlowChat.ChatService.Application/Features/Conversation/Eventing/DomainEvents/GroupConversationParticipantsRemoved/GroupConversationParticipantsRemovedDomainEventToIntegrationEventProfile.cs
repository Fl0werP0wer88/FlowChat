using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationParticipantsRemoved;

public sealed class GroupConversationParticipantsRemovedDomainEventToIntegrationEventProfile : Profile
{
    public GroupConversationParticipantsRemovedDomainEventToIntegrationEventProfile()
    {
        CreateMap<GroupConversationParticipantsRemovedDomainEvent, GroupConversationParticipantsRemovedIntegrationEvent>()
            .ForMember(destination => destination.ParticipantUserIds, options => options.MapFrom(source => source.ParticipantUserIds.Select(id => id.Value).ToList()))
            .ForMember(destination => destination.ConversationVersion, options => options.MapFrom(source => source.Version));
    }
}

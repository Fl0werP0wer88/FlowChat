using AutoMapper;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserProfileId.ToString()));
    }
}

using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<ContactAddedDomainEvent, ContactAddedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AggregateId.ToString()))
            .ForMember(destination => destination.OwnerUserId, options => options.MapFrom(source => source.OwnerUserId))
            .ForMember(destination => destination.ContactUserId, options => options.MapFrom(source => source.ContactUserId));

        CreateMap<ContactDeletedDomainEvent, ContactDeletedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AggregateId.ToString()))
            .ForMember(destination => destination.OwnerUserId, options => options.MapFrom(source => source.OwnerUserId))
            .ForMember(destination => destination.ContactUserId, options => options.MapFrom(source => source.ContactUserId));
    }
}

using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Eventing.DomainEvents.ContactDeleted;

public sealed class ContactDeletedDomainEventToIntegrationEventProfile : Profile
{
    public ContactDeletedDomainEventToIntegrationEventProfile()
    {
        CreateMap<ContactDeletedDomainEvent, ContactDeletedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AggregateId.ToString()))
            .ForMember(destination => destination.OwnerUserId, options => options.MapFrom(source => source.OwnerUserId))
            .ForMember(destination => destination.ContactUserId, options => options.MapFrom(source => source.ContactUserId));
    }
}

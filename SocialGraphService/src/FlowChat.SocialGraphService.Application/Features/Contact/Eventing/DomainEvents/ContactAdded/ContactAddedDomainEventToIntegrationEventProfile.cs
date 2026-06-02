using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Eventing.DomainEvents.ContactAdded;

public sealed class ContactAddedDomainEventToIntegrationEventProfile : Profile
{
    public ContactAddedDomainEventToIntegrationEventProfile()
    {
        CreateMap<ContactAddedDomainEvent, ContactAddedIntegrationEvent>()
            .ForMember(destination => destination.OwnerUserId, options => options.MapFrom(source => source.OwnerUserId.Value))
            .ForMember(destination => destination.ContactUserId, options => options.MapFrom(source => source.ContactUserId.Value));
    }
}

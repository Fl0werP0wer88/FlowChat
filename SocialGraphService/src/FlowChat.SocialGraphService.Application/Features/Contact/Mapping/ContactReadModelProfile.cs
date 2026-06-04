using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Mapping;

public sealed class ContactReadModelProfile : Profile
{
    public ContactReadModelProfile()
    {
        CreateMap<ContactAggregate, ContactReadModel>()
            .ForMember(destination => destination.ContactId, options => options.MapFrom(source => source.Id.Value))
            .ForMember(destination => destination.OwnerUserId, options => options.MapFrom(source => source.OwnerUserId.Value))
            .ForMember(destination => destination.ContactUserId, options => options.MapFrom(source => source.ContactUserId.Value))
            .ForMember(destination => destination.PhoneNumber, options => options.MapFrom(source => source.PhoneNumber == null ? null : source.PhoneNumber.Value))
            .ForMember(destination => destination.EmailAddress, options => options.MapFrom(source => source.EmailAddress == null ? null : source.EmailAddress.Value));
    }
}

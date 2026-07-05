using AutoMapper;
using FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;

public sealed class GetContactsForUserMappingProfile : Profile
{
    public GetContactsForUserMappingProfile()
    {
        CreateMap<ContactDto, ContactResponse>();
    }
}

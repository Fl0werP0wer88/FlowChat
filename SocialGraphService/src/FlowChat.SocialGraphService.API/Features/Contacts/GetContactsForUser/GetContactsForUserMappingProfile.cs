using FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contacts.GetContactsForUser;

public sealed class GetContactsForUserMappingProfile : Profile
{
    public GetContactsForUserMappingProfile()
    {
        CreateMap<GetContactsForUserRequest, GetContactsForUserQuery>()
            .ConstructUsing(source => new GetContactsForUserQuery(source.UserId));

        CreateMap<ContactDto, ContactResponse>();
    }
}

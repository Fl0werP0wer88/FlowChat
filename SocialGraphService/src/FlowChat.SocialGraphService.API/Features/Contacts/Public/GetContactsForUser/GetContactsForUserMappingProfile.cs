using FlowChat.SocialGraphService.Application.Features.Contacts.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contacts.Public.GetContactsForUser;

public sealed class GetContactsForUserMappingProfile : Profile
{
    public GetContactsForUserMappingProfile()
    {
        CreateMap<GetContactsForUserRequest, GetContactsForUserQuery>()
            .ConstructUsing(source => new GetContactsForUserQuery(source.UserId));
    }
}

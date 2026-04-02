using FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;

public sealed class GetContactsForUserMappingProfile : Profile
{
    public GetContactsForUserMappingProfile()
    {
        CreateMap<GetContactsForUserRequest, GetContactsForUserQuery>()
            .ConstructUsing(source => new GetContactsForUserQuery(source.UserId));
    }
}

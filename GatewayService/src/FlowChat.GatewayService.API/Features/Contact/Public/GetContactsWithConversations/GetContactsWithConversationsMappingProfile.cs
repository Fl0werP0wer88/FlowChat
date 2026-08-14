using AutoMapper;

namespace FlowChat.GatewayService.Api.Features.Contact.Public.GetContactsWithConversations;

public sealed class GetContactsWithConversationsMappingProfile : Profile
{
    public GetContactsWithConversationsMappingProfile()
    {
        CreateMap<ContactWithConversationResult, ContactWithConversationResponse>();
        CreateMap<GetContactsWithConversationsResult, GetContactsWithConversationsResponse>();
    }
}

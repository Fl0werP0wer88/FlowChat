using AutoMapper;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.GetDuetConversationsWithPresence;

public sealed class GetDuetConversationsWithPresenceMappingProfile : Profile
{
    public GetDuetConversationsWithPresenceMappingProfile()
    {
        CreateMap<DuetConversationWithPresenceResult, DuetConversationWithPresenceResponse>();
        CreateMap<GetDuetConversationsWithPresenceResult, GetDuetConversationsWithPresenceResponse>();
    }
}

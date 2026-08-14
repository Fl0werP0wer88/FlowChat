using AutoMapper;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesMappingProfile : Profile
{
    public CatchUpConversationMessagesMappingProfile()
    {
        CreateMap<ChatMessageClientDto, ChatMessageResponse>();
        CreateMap<CatchUpConversationMessagesResult, CatchUpConversationMessagesResponse>();
    }
}

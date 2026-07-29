using AutoMapper;
using FlowChat.GatewayService.Api.Services;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesMappingProfile : Profile
{
    public CatchUpConversationMessagesMappingProfile()
    {
        CreateMap<ChatMessageClientDto, ChatMessageResponse>();
        CreateMap<CatchUpConversationMessagesResult, CatchUpConversationMessagesResponse>();
    }
}

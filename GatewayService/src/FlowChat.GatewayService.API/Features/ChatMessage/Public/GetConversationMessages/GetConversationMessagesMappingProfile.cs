using AutoMapper;
using FlowChat.GatewayService.Api.Services;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesMappingProfile : Profile
{
    public GetConversationMessagesMappingProfile()
    {
        CreateMap<ChatMessageClientDto, ChatMessageResponse>();
        CreateMap<GetConversationMessagesResult, GetConversationMessagesResponse>();
    }
}

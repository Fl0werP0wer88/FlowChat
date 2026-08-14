using AutoMapper;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesMappingProfile : Profile
{
    public GetConversationMessagesMappingProfile()
    {
        CreateMap<ChatMessageClientDto, ChatMessageResponse>();
        CreateMap<GetConversationMessagesResult, GetConversationMessagesResponse>();
    }
}

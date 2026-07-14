using AutoMapper;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesMappingProfile : Profile
{
    public GetConversationMessagesMappingProfile()
    {
        CreateMap<ChatMessageDto, ChatMessageResponse>();
        CreateMap<ConversationMessagesPageDto, GetConversationMessagesResponse>();
    }
}

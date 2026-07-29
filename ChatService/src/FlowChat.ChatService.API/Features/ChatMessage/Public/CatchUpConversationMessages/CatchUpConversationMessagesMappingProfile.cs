using AutoMapper;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesMappingProfile : Profile
{
    public CatchUpConversationMessagesMappingProfile()
    {
        CreateMap<ChatMessageDto, ChatMessageResponse>();
        CreateMap<ConversationMessagesCatchUpPageDto, CatchUpConversationMessagesResponse>();
    }
}

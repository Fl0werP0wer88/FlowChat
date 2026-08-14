using AutoMapper;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.GetConversationMessagesRangeDescending;

public sealed class GetConversationMessagesRangeDescendingMappingProfile : Profile
{
    public GetConversationMessagesRangeDescendingMappingProfile()
    {
        CreateMap<ChatMessageDto, ChatMessageResponse>();
        CreateMap<ConversationMessagesRangeDescendingPageDto, GetConversationMessagesRangeDescendingResponse>();
    }
}

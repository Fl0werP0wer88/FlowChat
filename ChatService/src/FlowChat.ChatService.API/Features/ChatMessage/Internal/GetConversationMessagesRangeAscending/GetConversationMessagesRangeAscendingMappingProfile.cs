using AutoMapper;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.GetConversationMessagesRangeAscending;

public sealed class GetConversationMessagesRangeAscendingMappingProfile : Profile
{
    public GetConversationMessagesRangeAscendingMappingProfile()
    {
        CreateMap<ChatMessageDto, ChatMessageResponse>();
        CreateMap<ConversationMessagesRangeAscendingPageDto, GetConversationMessagesRangeAscendingResponse>();
    }
}

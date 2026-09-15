using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversations;

public sealed class GetDuetConversationsMappingProfile : Profile
{
    public GetDuetConversationsMappingProfile()
    {
        CreateMap<DuetConversationListItemDto, DuetConversationResponse>();
    }
}

using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversations;

public sealed class GetGroupConversationsMappingProfile : Profile
{
    public GetGroupConversationsMappingProfile()
    {
        CreateMap<GroupConversationSummaryDto, GroupConversationSummaryResponse>();
    }
}

using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CopyDuetAsGroup;

public sealed class CopyDuetAsGroupMappingProfile : Profile
{
    public CopyDuetAsGroupMappingProfile()
    {
        CreateMap<ConversationParticipantDto, ParticipantResponse>();
        CreateMap<GroupConversationDetailDto, CopyDuetAsGroupResponse>();
    }
}

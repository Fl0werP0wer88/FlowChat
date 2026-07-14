using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversation;

public sealed class GetGroupConversationMappingProfile : Profile
{
    public GetGroupConversationMappingProfile()
    {
        CreateMap<ConversationParticipantDto, ParticipantResponse>();
        CreateMap<GroupConversationDetailDto, GetGroupConversationResponse>();
    }
}

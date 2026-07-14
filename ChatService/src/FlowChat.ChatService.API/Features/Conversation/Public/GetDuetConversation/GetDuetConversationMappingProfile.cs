using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversation;

public sealed class GetDuetConversationMappingProfile : Profile
{
    public GetDuetConversationMappingProfile()
    {
        CreateMap<ConversationParticipantDto, ParticipantResponse>();
        CreateMap<DuetConversationDetailDto, GetDuetConversationResponse>();
    }
}

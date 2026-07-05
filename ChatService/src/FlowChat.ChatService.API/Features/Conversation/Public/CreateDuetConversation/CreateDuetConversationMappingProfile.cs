using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateDuetConversation;

public sealed class CreateDuetConversationMappingProfile : Profile
{
    public CreateDuetConversationMappingProfile()
    {
        CreateMap<ConversationParticipantDto, ParticipantResponse>();
        CreateMap<DuetConversationDetailDto, CreateDuetConversationResponse>();
    }
}

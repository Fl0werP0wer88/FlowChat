using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateGroupConversation;

public sealed class CreateGroupConversationMappingProfile : Profile
{
    public CreateGroupConversationMappingProfile()
    {
        CreateMap<ConversationParticipantDto, ParticipantResponse>();
        CreateMap<GroupConversationDetailDto, CreateGroupConversationResponse>();
    }
}

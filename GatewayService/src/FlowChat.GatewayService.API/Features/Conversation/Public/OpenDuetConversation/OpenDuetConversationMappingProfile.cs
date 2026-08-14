using AutoMapper;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;

public sealed class OpenDuetConversationMappingProfile : Profile
{
    public OpenDuetConversationMappingProfile()
    {
        CreateMap<ConversationParticipantClientDto, ConversationParticipantResponse>();
        CreateMap<ChatMessageClientDto, ConversationMessageResponse>();
        CreateMap<OpenDuetConversationResult, OpenDuetConversationResponse>();
    }
}

using AutoMapper;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;

public sealed class OpenGroupConversationMappingProfile : Profile
{
    public OpenGroupConversationMappingProfile()
    {
        CreateMap<ConversationParticipantClientDto, ConversationParticipantResponse>();
        CreateMap<ChatMessageClientDto, ConversationMessageResponse>();
        CreateMap<OpenGroupConversationResult, OpenGroupConversationResponse>();
    }
}

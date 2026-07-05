using AutoMapper;
using FlowChat.GatewayService.Api.Services;

namespace FlowChat.GatewayService.Api.Models;

public sealed class ConversationAggregateMappingProfile : Profile
{
    public ConversationAggregateMappingProfile()
    {
        CreateMap<ConversationParticipantClientDto, ConversationParticipantResponse>();
        CreateMap<ChatMessageClientDto, ConversationMessageResponse>();
    }
}

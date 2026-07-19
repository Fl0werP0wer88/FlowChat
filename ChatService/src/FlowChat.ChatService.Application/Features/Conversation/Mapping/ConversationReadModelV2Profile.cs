using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class ConversationReadModelV2Profile : Profile
{
    public ConversationReadModelV2Profile()
    {
        CreateMap<ConversationV2, ConversationReadModelV2>()
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.Id.Value))
            .ForMember(x => x.ConversationType, options => options.MapFrom(x => (int)x.ConversationType))
            .ForMember(x => x.CreatedByUserId, options => options.MapFrom(x => x.CreatedByUserId.Value));
    }
}

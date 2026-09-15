using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class ConversationReadModelV2Profile : Profile
{
    public ConversationReadModelV2Profile()
    {
        CreateMap<ConversationV2, ConversationReadModelV2>()
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.Id.Value))
            .ForMember(x => x.ConversationType, options => options.MapFrom(x => (int)x.ConversationType))
            .ForMember(
                x => x.DuetFirstUserId,
                options => options.MapFrom(x => x.DuetParticipants == null
                    ? (Guid?)null
                    : x.DuetParticipants.FirstUserId.Value))
            .ForMember(
                x => x.DuetSecondUserId,
                options => options.MapFrom(x => x.DuetParticipants == null
                    ? (Guid?)null
                    : x.DuetParticipants.SecondUserId.Value));
    }
}

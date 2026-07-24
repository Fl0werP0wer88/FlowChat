using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class ConversationParticipantReadModelV2Profile : Profile
{
    public ConversationParticipantReadModelV2Profile()
    {
        CreateMap<ConversationParticipant, ConversationMembershipReadModelV2>()
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.ConversationId.Value))
            .ForMember(x => x.ParticipantUserId, options => options.MapFrom(x => x.UserId.Value));

        CreateMap<ConversationParticipant, ConversationParticipantReadModelV2>()
            .ForMember(x => x.ParticipantId, options => options.MapFrom(x => x.Id.Value))
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.ConversationId.Value))
            .ForMember(x => x.ConversationType, options => options.MapFrom(x => (int)x.ConversationType))
            .ForMember(x => x.UserId, options => options.MapFrom(x => x.UserId.Value))
            .ForMember(x => x.JoinedAtUtc, options => options.MapFrom(x => x.JoinedAtUtc.Value));
    }
}

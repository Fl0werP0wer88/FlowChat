using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class ConversationParticipantIntegrationEventProfile : Profile
{
    public ConversationParticipantIntegrationEventProfile()
    {
        CreateMap<ConversationParticipant, ConversationParticipantChangedIntegrationEventV2>()
            .ForMember(x => x.ParticipantId, options => options.MapFrom(x => x.Id.Value))
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.ConversationId.Value))
            .ForMember(x => x.UserId, options => options.MapFrom(x => x.UserId.Value))
            .ForMember(x => x.JoinedAtUtc, options => options.MapFrom(x => x.JoinedAtUtc.Value))
            .ForMember(x => x.CreatedAtUtc, options => options.MapFrom(x => x.CreatedAtUtc.Value))
            .ForMember(x => x.ModifiedAtUtc, options => options.MapFrom(x => x.LastModifiedAtUtc.Value))
            .ForMember(x => x.DeletedAtUtc, options => options.MapFrom(x => x.DeletedAt == null ? null : (DateTimeOffset?)x.DeletedAt.Value))
            .ForMember(x => x.Operation, options => options.Ignore());
    }
}

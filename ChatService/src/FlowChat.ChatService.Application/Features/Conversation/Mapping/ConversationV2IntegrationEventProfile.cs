using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class ConversationV2IntegrationEventProfile : Profile
{
    public ConversationV2IntegrationEventProfile()
    {
        CreateMap<ConversationV2, ConversationChangedIntegrationEventV2>()
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.Id.Value))
            .ForMember(x => x.ConversationType, options => options.MapFrom(x => (int)x.ConversationType))
            .ForMember(x => x.CreatedByUserId, options => options.MapFrom(x => x.CreatedByUserId.Value))
            .ForMember(x => x.CreatedAtUtc, options => options.MapFrom(x => x.CreatedAtUtc.Value))
            .ForMember(x => x.ModifiedAtUtc, options => options.MapFrom(x => x.LastModifiedAtUtc.Value))
            .ForMember(x => x.DeletedAtUtc, options => options.MapFrom(x => x.DeletedAt == null ? null : (DateTimeOffset?)x.DeletedAt.Value))
            .ForMember(x => x.Operation, options => options.Ignore());
    }
}

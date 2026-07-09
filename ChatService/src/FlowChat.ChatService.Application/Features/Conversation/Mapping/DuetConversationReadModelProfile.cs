using AutoMapper;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class DuetConversationReadModelProfile : Profile
{
    public DuetConversationReadModelProfile()
    {
        CreateMap<DuetConversationAggregate, DuetConversationReadModel>()
            .ForMember(destination => destination.ConversationId, options => options.MapFrom(source => source.Id.Value))
            .ForMember(destination => destination.FirstUserId, options => options.MapFrom(source => source.GetParticipantPair().FirstUserId.Value))
            .ForMember(destination => destination.SecondUserId, options => options.MapFrom(source => source.GetParticipantPair().SecondUserId.Value));
    }
}

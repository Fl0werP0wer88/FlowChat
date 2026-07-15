using AutoMapper;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class DuetConversationContactStateReadModelProfile : Profile
{
    public DuetConversationContactStateReadModelProfile()
    {
        CreateMap<DuetConversationAggregate, DuetConversationContactStateReadModel>()
            .ForMember(destination => destination.ConversationId, options => options.MapFrom(source => source.Id.Value))
            .ForMember(destination => destination.FirstUserId, options => options.MapFrom(source => source.GetParticipantPair().FirstUserId.Value))
            .ForMember(destination => destination.SecondUserId, options => options.MapFrom(source => source.GetParticipantPair().SecondUserId.Value))
            .ForMember(destination => destination.FirstUserBlockedSecondUser, options => options.MapFrom(source =>
                source.GetParticipant(source.GetParticipantPair().FirstUserId)!.IsBlocked))
            .ForMember(destination => destination.SecondUserBlockedFirstUser, options => options.MapFrom(source =>
                source.GetParticipant(source.GetParticipantPair().SecondUserId)!.IsBlocked));
    }
}

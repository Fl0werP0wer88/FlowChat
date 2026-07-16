using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Application.Features.Conversation.Mapping;

public sealed class GroupConversationMembershipReadModelProfile : Profile
{
    public GroupConversationMembershipReadModelProfile()
    {
        CreateMap<GroupConversation, IEnumerable<ParticipantUser>>()
            .ConvertUsing(source => source.Participants);
        CreateMap<ParticipantUser, GroupConversationMembershipReadModel>()
            .ForMember(destination => destination.ConversationId, options => options.MapFrom(source => source.ConversationId.Value))
            .ForMember(destination => destination.ParticipantUserId, options => options.MapFrom(source => source.UserId.Value));
    }
}

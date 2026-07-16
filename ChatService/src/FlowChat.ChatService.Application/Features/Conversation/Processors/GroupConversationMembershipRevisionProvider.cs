using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors.Interfaces;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class GroupConversationMembershipRevisionProvider
    : IDeltaProjectionRevisionProvider<GroupConversationAggregate, GroupConversationMembershipReadModel>
{
    public int GetRevision(GroupConversationAggregate aggregate) => aggregate.MembershipRevision;
}

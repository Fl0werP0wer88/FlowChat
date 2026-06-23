using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversations;

public sealed record GetGroupConversationsQuery(Guid ParticipantUserId)
    : IQuery<IReadOnlyCollection<GroupConversationSummaryDto>>;

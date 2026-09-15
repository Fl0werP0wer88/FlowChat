using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversations;

public sealed record GetDuetConversationsQuery(
    Guid RequestingUserId) : IQuery<IReadOnlyCollection<DuetConversationListItemDto>>;

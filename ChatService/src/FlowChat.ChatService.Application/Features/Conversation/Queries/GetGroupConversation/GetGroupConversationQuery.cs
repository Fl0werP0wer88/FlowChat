using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversation;

public sealed record GetGroupConversationQuery(Guid ConversationId) : IQuery<GroupConversationDetailDto>;

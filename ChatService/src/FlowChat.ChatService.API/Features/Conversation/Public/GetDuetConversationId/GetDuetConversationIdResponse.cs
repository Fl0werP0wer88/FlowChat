using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationId;

public sealed record GetDuetConversationIdResponse(Guid ConversationId) : IServiceOutput;

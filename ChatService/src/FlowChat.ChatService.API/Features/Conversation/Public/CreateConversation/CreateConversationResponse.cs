using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateConversation;

public sealed record CreateConversationResponse(Guid ConversationId) : IServiceOutput;

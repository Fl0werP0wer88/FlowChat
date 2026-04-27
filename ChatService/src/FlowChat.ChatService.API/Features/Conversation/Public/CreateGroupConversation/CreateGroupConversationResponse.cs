using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateGroupConversation;

public sealed record CreateGroupConversationResponse(Guid ConversationId) : IServiceOutput;

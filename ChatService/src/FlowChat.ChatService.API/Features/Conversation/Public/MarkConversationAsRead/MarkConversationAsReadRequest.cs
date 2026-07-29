using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.MarkConversationAsRead;

public sealed record MarkConversationAsReadRequest(long SequenceNum) : IServiceInput;

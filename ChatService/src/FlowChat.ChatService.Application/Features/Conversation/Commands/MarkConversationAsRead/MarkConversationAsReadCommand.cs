using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed record MarkConversationAsReadCommand(
    Guid ConversationId,
    Guid ParticipantUserId) : ICommand<Unit>;

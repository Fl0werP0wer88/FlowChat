using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed record MarkConversationAsReadCommandV2(
    Guid ConversationId,
    Guid ParticipantUserId) : ICommand<Unit>;

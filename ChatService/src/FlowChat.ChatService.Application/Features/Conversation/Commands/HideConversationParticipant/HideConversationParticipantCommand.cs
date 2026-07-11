using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.HideConversationParticipant;

public sealed record HideConversationParticipantCommand(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;

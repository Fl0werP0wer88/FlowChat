using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.UnhideConversationParticipant;

public sealed record UnhideConversationParticipantCommand(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;

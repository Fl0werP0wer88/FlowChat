using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.BlockConversationParticipant;

public sealed record BlockConversationParticipantCommand(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;

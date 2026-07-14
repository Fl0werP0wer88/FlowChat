using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;

public sealed record UnblockConversationParticipantCommand(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;

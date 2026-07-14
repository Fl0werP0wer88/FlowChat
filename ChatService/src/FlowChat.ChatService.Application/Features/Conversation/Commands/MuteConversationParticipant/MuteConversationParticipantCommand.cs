using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MuteConversationParticipant;

public sealed record MuteConversationParticipantCommand(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;

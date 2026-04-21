using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed record AddParticipantCommand(
    Guid ConversationId,
    Guid ParticipantUserId) : ICommand<Unit>;

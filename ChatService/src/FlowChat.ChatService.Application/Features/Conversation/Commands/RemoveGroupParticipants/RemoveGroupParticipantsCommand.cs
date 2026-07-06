using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed record RemoveGroupParticipantsCommand(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds) : ICommand<Unit>;

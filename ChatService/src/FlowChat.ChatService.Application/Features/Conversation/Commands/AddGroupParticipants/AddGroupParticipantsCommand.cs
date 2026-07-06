using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed record AddGroupParticipantsCommand(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds) : ICommand<Unit>;

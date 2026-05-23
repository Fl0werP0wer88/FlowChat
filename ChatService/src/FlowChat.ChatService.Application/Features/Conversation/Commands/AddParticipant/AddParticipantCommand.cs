using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed record AddParticipantCommand(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds) : ICommand<IdempotentCommandResult<bool>>
{
    public const string IdempotencyConflictKey = nameof(AddParticipantCommand);
}

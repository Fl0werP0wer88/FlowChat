using FlowChat.Shared.Application;
using FlowChat.Core.Results;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed record SendChatMessageCommand(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text) : ICommand<IdempotentCommandResult<Guid>>
{
    public const string IdempotencyConflictKey = nameof(SendChatMessageCommand);
}

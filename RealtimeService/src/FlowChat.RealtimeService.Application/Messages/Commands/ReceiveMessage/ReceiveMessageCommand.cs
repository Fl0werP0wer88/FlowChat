using MediatR;

namespace FlowChat.RealtimeService.Application.Messages.Commands.ReceiveMessage;

public sealed record ReceiveMessageCommand(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text,
    DateTime SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : IRequest;

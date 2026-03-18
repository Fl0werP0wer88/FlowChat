using MediatR;

namespace FlowChat.RealtimeService.Application.Messages.Commands.PublishMessage;

public sealed record PublishMessageCommand(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text,
    DateTime SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : IRequest;

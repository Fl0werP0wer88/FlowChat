using FlowChat.Application.Abstractions;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Messages.Commands.PublishMessage;

public sealed record PublishMessageCommand(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text,
    DateTime SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Unit>;

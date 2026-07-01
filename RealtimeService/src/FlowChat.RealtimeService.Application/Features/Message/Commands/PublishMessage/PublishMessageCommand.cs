using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;

public sealed record PublishMessageCommand(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text,
    long SequenceNum,
    DateTimeOffset SentAtUtc,
    DateTimeOffset DeliveredAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Unit>;


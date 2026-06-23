using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;

public sealed record RouteMessageCommand(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text,
    DateTimeOffset SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Unit>;

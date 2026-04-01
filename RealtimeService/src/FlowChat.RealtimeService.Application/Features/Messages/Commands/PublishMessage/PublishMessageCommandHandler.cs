using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain;
using FlowChat.RealtimeService.Domain.Notifications;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Messages.Commands.PublishMessage;

public sealed class PublishMessageCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishMessageCommand, Unit>
{
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(PublishMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.MessageId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("MessageId is required."));
        }

        if (request.ConversationId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("ConversationId is required."));
        }

        if (request.SenderUserId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("SenderUserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.SenderDisplayName))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("SenderDisplayName is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Text is required."));
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        if (recipientUserIds.Length == 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("RecipientUserIds must contain at least one valid user id."));
        }

        var notification = new ChatMessageNotification(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName.Trim(),
            request.Text.Trim(),
            request.SentAtUtc,
            recipientUserIds);

        await _realtimeClientDispatcher.ReceiveMessageAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeRecipientUserIds(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}


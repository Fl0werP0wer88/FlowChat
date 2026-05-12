using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;

public sealed class PublishMessageCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishMessageCommand, Unit>
{
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(PublishMessageCommand request, CancellationToken cancellationToken)
    {
        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);

        var notification = new ChatMessageParam(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName!.Trim(),
            request.Text!.Trim(),
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

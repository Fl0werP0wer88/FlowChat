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
    //Ogólnie z tego co widze to nie bedzie się dało zapenić exactly once delivery po stronie backendu (SignalR nie pozwoli max co mozna zrobic to zapisywac wwpis do jakiegos outboxa i worker niech wysyła do kliena ale to nadal nie bedzie w pełni excatly once). Pamiętać żeby dodać deduplikację po stronie klienta.
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
            request.DeliveredAtUtc,
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

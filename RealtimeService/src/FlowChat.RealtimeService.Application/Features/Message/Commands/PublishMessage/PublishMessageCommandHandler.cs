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
        var notification = new ChatMessageParam(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.Text!.Trim(),
            request.SequenceNum,
            request.SentAtUtc,
            request.DeliveredAtUtc,
            []);

        await _realtimeClientDispatcher.MessageReceivedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}

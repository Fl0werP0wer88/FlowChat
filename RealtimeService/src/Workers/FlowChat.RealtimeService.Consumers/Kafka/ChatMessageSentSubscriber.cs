using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ChatMessageSentSubscriber(
    IMediator mediator,
    ILogger<ChatMessageSentSubscriber> logger)
    : SubscriberBase<ChatMessageSentIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        ChatMessageSentIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var command = new RouteMessageCommand(
            message.MessageId,
            message.ConversationId,
            message.SenderUserId,
            message.Text?.Trim(),
            message.SentAtUtc,
            (message.RecipientUserIds ?? [])
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray(),
            message.ConversationVersionAtSend);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}

using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ChatMessageSentV2Subscriber(
    IMediator mediator,
    ILogger<ChatMessageSentV2Subscriber> logger)
    : SubscriberBase<ChatMessageSentIntegrationEventV2>(logger)
{
    protected override async Task ExecuteAsync(
        ChatMessageSentIntegrationEventV2 message,
        CancellationToken cancellationToken)
    {
        var command = new RouteMessageCommand(
            message.MessageId,
            message.ConversationId,
            message.SenderUserId,
            message.Text?.Trim(),
            message.SentAtUtc,
            message.ConversationMembershipRevision);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}

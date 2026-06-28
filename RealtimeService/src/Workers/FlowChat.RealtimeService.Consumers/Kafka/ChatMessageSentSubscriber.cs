using FlowChat.Core.Exceptions;
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
        Validate(message);

        var command = new RouteMessageCommand(
            message.MessageId,
            message.ConversationId,
            message.SenderUserId,
            message.SenderDisplayName.Trim(),
            message.Text.Trim(),
            message.SentAtUtc,
            message.RecipientUserIds
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray());

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }

    private static void Validate(ChatMessageSentIntegrationEvent message)
    {
        if (message.MessageId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid MessageId.");
        }

        if (message.ConversationId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid ConversationId.");
        }

        if (message.SenderUserId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid SenderUserId.");
        }

        if (string.IsNullOrWhiteSpace(message.SenderDisplayName))
        {
            throw new NonTransientException("Payload does not contain valid SenderDisplayName.");
        }

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            throw new NonTransientException("Payload does not contain valid Text.");
        }

        if (message.RecipientUserIds is null || !message.RecipientUserIds.Any(userId => userId != Guid.Empty))
        {
            throw new NonTransientException("Payload does not contain valid RecipientUserIds.");
        }
    }
}

using FlowChat.Core.Exceptions;
using FlowChat.Messaging.Contracts.ChatService.Events;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using Silverback.Messaging.Subscribers;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ChatMessageSentSubscriber(
    IRealtimeInternalApiClient realtimeInternalApiClient,
    ILogger<ChatMessageSentSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(ChatMessageSentIntegrationEvent message, CancellationToken cancellationToken)
    {
        Validate(message);

        try
        {
            var request = new PublishMessageRequest
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                SenderUserId = message.SenderUserId,
                SenderDisplayName = message.SenderDisplayName.Trim(),
                Text = message.Text.Trim(),
                SentAtUtc = message.SentAtUtc,
                RecipientUserIds = message.RecipientUserIds
                    .Where(userId => userId != Guid.Empty)
                    .Distinct()
                    .ToArray()
            };

            await realtimeInternalApiClient.PublishMessageAsync(request, cancellationToken);
        }
        catch (NonTransientException exception)
        {
            logger.LogInformation(
                exception,
                "Skipping forwarding {EventType} event for message {MessageId}. Reason: {Reason}",
                nameof(ChatMessageSentIntegrationEvent),
                message.MessageId,
                exception.Message);

            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to forward {EventType} event for message {MessageId} to RealtimeService API.",
                nameof(ChatMessageSentIntegrationEvent),
                message.MessageId);

            throw;
        }
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

using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ChatMessageSentSubscriber(
    IRealtimeInternalApiClient realtimeInternalApiClient,
    ILogger<ChatMessageSentSubscriber> logger)
    : SubscriberBase<ChatMessageSentIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        ChatMessageSentIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        Validate(message);

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

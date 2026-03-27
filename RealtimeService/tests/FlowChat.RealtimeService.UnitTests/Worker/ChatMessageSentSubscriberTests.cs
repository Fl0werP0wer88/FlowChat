using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Consumers.Kafka;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatMessageSentSubscriberTests
{
    [Fact]
    public async Task HandleAsync_ForwardsMappedRequestToInternalApi()
    {
        var internalApiClient = new CapturingRealtimeInternalApiClient();
        var subscriber = new ChatMessageSentSubscriber(
            internalApiClient,
            NullLogger<ChatMessageSentSubscriber>.Instance);
        var recipientUserId = Guid.NewGuid();

        await subscriber.HandleAsync(
            new ChatMessageSentIntegrationEvent
            {
                MessageId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                SenderUserId = Guid.NewGuid(),
                SenderDisplayName = " Jane Doe ",
                Text = " Hi there ",
                SentAtUtc = new DateTime(2026, 3, 17, 10, 0, 0, DateTimeKind.Utc),
                RecipientUserIds = [recipientUserId, recipientUserId]
            },
            CancellationToken.None);

        Assert.NotNull(internalApiClient.LastPublishMessageRequest);
        Assert.Equal("Jane Doe", internalApiClient.LastPublishMessageRequest!.SenderDisplayName);
        Assert.Equal("Hi there", internalApiClient.LastPublishMessageRequest.Text);
        Assert.Single(internalApiClient.LastPublishMessageRequest.RecipientUserIds);
    }

    [Fact]
    public async Task HandleAsync_WhenMessageIdMissing_ThrowsNonTransientException()
    {
        var internalApiClient = new CapturingRealtimeInternalApiClient();
        var subscriber = new ChatMessageSentSubscriber(
            internalApiClient,
            NullLogger<ChatMessageSentSubscriber>.Instance);

        await Assert.ThrowsAsync<NonTransientException>(() => subscriber.HandleAsync(
            new ChatMessageSentIntegrationEvent
            {
                MessageId = Guid.Empty,
                ConversationId = Guid.NewGuid(),
                SenderUserId = Guid.NewGuid(),
                SenderDisplayName = "Jane Doe",
                Text = "Hi there",
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None));

        Assert.Null(internalApiClient.LastPublishMessageRequest);
    }
}

using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatMessageSentSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _eventRouterMock = new();
    private readonly ChatMessageSentSubscriber _subscriber;

    public ChatMessageSentSubscriberTests()
    {
        _subscriber = new ChatMessageSentSubscriber(
            _eventRouterMock.Object,
            NullLogger<ChatMessageSentSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsMappedRequestToRouter()
    {
        PublishMessageRequest? capturedRequest = null;
        var recipientUserId = _fixture.Create<Guid>();

        _eventRouterMock
            .Setup(x => x.PublishMessageAsync(It.IsAny<PublishMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishMessageRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new ChatMessageSentIntegrationEvent
            {
                MessageId = _fixture.Create<Guid>(),
                ConversationId = _fixture.Create<Guid>(),
                SenderUserId = _fixture.Create<Guid>(),
                SenderDisplayName = " Jane Doe ",
                Text = " Hi there ",
                SentAtUtc = new DateTimeOffset(2026, 3, 17, 10, 0, 0, TimeSpan.Zero),
                RecipientUserIds = [recipientUserId, recipientUserId]
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.SenderDisplayName.Should().Be("Jane Doe");
        capturedRequest.Text.Should().Be("Hi there");
        capturedRequest.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenMessageIdMissing_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new ChatMessageSentIntegrationEvent
            {
                MessageId = Guid.Empty,
                ConversationId = _fixture.Create<Guid>(),
                SenderUserId = _fixture.Create<Guid>(),
                SenderDisplayName = "Jane Doe",
                Text = "Hi there",
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _eventRouterMock.Verify(
            x => x.PublishMessageAsync(It.IsAny<PublishMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

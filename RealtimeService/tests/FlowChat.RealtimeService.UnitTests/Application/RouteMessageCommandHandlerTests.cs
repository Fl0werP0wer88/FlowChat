using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteMessageCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly Mock<IChatServiceInternalApiClient> _chatServiceApiClientMock = new();
    private readonly RouteMessageCommandHandler _handler;

    public RouteMessageCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _chatServiceApiClientMock
            .Setup(x => x.MarkChatMessageAsDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteMessageCommandHandler(_routerMock.Object, _chatServiceApiClientMock.Object);
    }

    [Fact]
    public async Task Handle_MapsNotificationAndRoutesToRecipients()
    {
        ChatMessageParam? capturedNotification = null;
        DateTimeOffset? deliveredAtUtc = null;
        var recipientUserId = _fixture.Create<Guid>();
        var messageId = _fixture.Create<Guid>();
        var conversationId = _fixture.Create<Guid>();

        _routerMock
            .Setup(x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        _chatServiceApiClientMock
            .Setup(x => x.MarkChatMessageAsDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DateTimeOffset, CancellationToken>((_, _, value, _) => deliveredAtUtc = value)
            .Returns(Task.CompletedTask);

        var beforeHandleUtc = DateTimeOffset.UtcNow;
        var result = await _handler.Handle(
            new RouteMessageCommand(
                messageId,
                conversationId,
                _fixture.Create<Guid>(),
                " John Doe ",
                " Hello there ",
                new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero),
                [recipientUserId, recipientUserId, Guid.Empty]),
            CancellationToken.None);
        var afterHandleUtc = DateTimeOffset.UtcNow;

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.SenderDisplayName.Should().Be("John Doe");
        capturedNotification.Text.Should().Be("Hello there");
        capturedNotification.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
        capturedNotification.DeliveredAtUtc.Offset.Should().Be(TimeSpan.Zero);
        deliveredAtUtc.Should().NotBeNull();
        deliveredAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        capturedNotification.DeliveredAtUtc.Should().Be(deliveredAtUtc.Value);
        deliveredAtUtc.Value.Should().BeOnOrAfter(beforeHandleUtc);
        deliveredAtUtc.Value.Should().BeOnOrBefore(afterHandleUtc);
        _chatServiceApiClientMock.Verify(
            x => x.MarkChatMessageAsDeliveredAsync(messageId, conversationId, deliveredAtUtc.Value, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

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
        _chatServiceApiClientMock
            .Setup(x => x.SetChatMessageSequenceNumberAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

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
        const long sequenceNum = 42;
        var sequence = new MockSequence();

        _chatServiceApiClientMock.InSequence(sequence)
            .Setup(x => x.SetChatMessageSequenceNumberAsync(
                messageId,
                conversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNum);

        _routerMock.InSequence(sequence)
            .Setup(x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        _chatServiceApiClientMock.InSequence(sequence)
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
                " Hello there ",
                new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero),
                [recipientUserId, recipientUserId, Guid.Empty]),
            CancellationToken.None);
        var afterHandleUtc = DateTimeOffset.UtcNow;

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Text.Should().Be("Hello there");
        capturedNotification.SequenceNum.Should().Be(sequenceNum);
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

    [Fact]
    public async Task Handle_WhenSetSequenceNumberFails_DoesNotRouteMessageOrMarkDelivered()
    {
        _chatServiceApiClientMock
            .Setup(x => x.SetChatMessageSequenceNumberAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = () => _handler.Handle(CreateValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");
        _routerMock.Verify(
            x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _chatServiceApiClientMock.Verify(
            x => x.MarkChatMessageAsDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRouteMessageFails_DoesNotMarkDelivered()
    {
        _routerMock
            .Setup(x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = () => _handler.Handle(CreateValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");
        _chatServiceApiClientMock.Verify(
            x => x.MarkChatMessageAsDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RouteMessageCommand CreateValidCommand() =>
        new(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "Hello there",
            new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero),
            [_fixture.Create<Guid>()]);
}

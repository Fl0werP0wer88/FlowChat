using AutoFixture;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.Shared.Domain;
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
            .Setup(x => x.MarkMessageProcessedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteMessageCommandHandler(_routerMock.Object, _chatServiceApiClientMock.Object);
    }

    [Fact]
    public async Task Handle_MapsNotificationAndRoutesToRecipients()
    {
        ChatMessageParam? capturedNotification = null;
        var recipientUserId = _fixture.Create<Guid>();

        _routerMock
            .Setup(x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RouteMessageCommand(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                " John Doe ",
                " Hello there ",
                new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero),
                [recipientUserId, recipientUserId, Guid.Empty]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.SenderDisplayName.Should().Be("John Doe");
        capturedNotification.Text.Should().Be("Hello there");
        capturedNotification.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
    }

    [Fact]
    public async Task Handle_WhenRecipientsMissing_ReturnsBadRequestFailure()
    {
        var result = await _handler.Handle(
            new RouteMessageCommand(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                "John Doe",
                "Hello",
                DateTimeOffset.UtcNow,
                [Guid.Empty]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        _routerMock.Verify(
            x => x.RouteMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

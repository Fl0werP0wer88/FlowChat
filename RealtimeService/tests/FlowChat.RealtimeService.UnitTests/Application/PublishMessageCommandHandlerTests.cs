using AutoFixture;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishMessageCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();
    private readonly PublishMessageCommandHandler _handler;

    public PublishMessageCommandHandlerTests()
    {
        _dispatcherMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ChatMessageNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new PublishMessageCommandHandler(_dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_MapsNotificationAndDispatchesToRecipients()
    {
        ChatMessageNotification? capturedNotification = null;
        var recipientUserId = _fixture.Create<Guid>();

        _dispatcherMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ChatMessageNotification>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageNotification, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new PublishMessageCommand(
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
            new PublishMessageCommand(
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
        _dispatcherMock.Verify(
            x => x.ReceiveMessageAsync(It.IsAny<ChatMessageNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

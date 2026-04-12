using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishPresenceChangeCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();
    private readonly PublishPresenceChangeCommandHandler _handler;

    public PublishPresenceChangeCommandHandlerTests()
    {
        _dispatcherMock
            .Setup(x => x.PresenceChangedAsync(It.IsAny<PresenceChangedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new PublishPresenceChangeCommandHandler(_dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesStatusAndDispatches()
    {
        PresenceChangedNotification? capturedNotification = null;
        var recipientUserId = _fixture.Create<Guid>();

        _dispatcherMock
            .Setup(x => x.PresenceChangedAsync(It.IsAny<PresenceChangedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceChangedNotification, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new PublishPresenceChangeCommand(
                _fixture.Create<Guid>(),
                PresenceStatus.Active,
                new DateTimeOffset(2026, 3, 17, 12, 30, 0, TimeSpan.Zero),
                [recipientUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(PresenceStatus.Active);
        capturedNotification.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
    }

    [Fact]
    public async Task Handle_WhenStatusIsInvalid_ReturnsBadRequestFailure()
    {
        var result = await _handler.Handle(
            new PublishPresenceChangeCommand(_fixture.Create<Guid>(), (PresenceStatus)999, DateTimeOffset.UtcNow, [_fixture.Create<Guid>()]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        _dispatcherMock.Verify(
            x => x.PresenceChangedAsync(It.IsAny<PresenceChangedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

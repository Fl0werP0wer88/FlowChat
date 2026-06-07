using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class ChangePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ChangePresenceStatusCommandHandler _handler;

    public ChangePresenceStatusCommandHandlerTests()
    {
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChangeUserPresencePreferencesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _handler = new ChangePresenceStatusCommandHandler(
            _presenceStatusStoreMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStatusChanges_StoresStatusAndPublishesApplicationEvent()
    {
        var userId = _fixture.Create<Guid>();
        var previous = new PresenceStatusSnapshot(userId, PresenceStatus.Active, DateTimeOffset.UtcNow.AddMinutes(-5));
        PresenceStatusChangedApplicationEvent? capturedNotification = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedApplicationEvent, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Busy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.UserId.Should().Be(userId);
        capturedNotification.Status.Should().Be(PresenceStatus.Busy);
        capturedNotification.ChangedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Busy, capturedNotification.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStatusIsUnchanged_ReturnsSuccessWithoutPublishing()
    {
        var userId = _fixture.Create<Guid>();
        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Invisible, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Invisible),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(It.IsAny<Guid>(), It.IsAny<PresenceStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenApplicationEventPublishFails_RestoresPreviousStatus()
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = new PresenceStatusSnapshot(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-10));

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousStatus);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Busy),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Busy, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, previousStatus.Status, previousStatus.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApplicationEventPublishFailsWithoutPreviousStatus_DeletesRedisEntry()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Active),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(PresenceStatus.Active)]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task Handle_WithExplicitStatus_DispatchesChangeUserPresencePreferencesCommand(PresenceStatus status)
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = status == PresenceStatus.Active ? PresenceStatus.Busy : PresenceStatus.Active;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, previousStatus, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, status),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<ChangeUserPresencePreferencesCommand>(cmd => cmd.UserId == userId && cmd.Status == status),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithAFKStatus_DoesNotDispatchPreferenceCommands()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Active, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.AFK),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<ChangeUserPresencePreferencesCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRollbackFails_ReturnsFailureInsteadOfThrowing()
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = new PresenceStatusSnapshot(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-3));

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousStatus);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));
        _presenceStatusStoreMock
            .Setup(x => x.SetAsync(userId, previousStatus.Status, previousStatus.ChangedAtUtc, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rollback failed"));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Busy),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
    }
}

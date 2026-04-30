using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class ChangePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IUserPresencePreferencesWriteRepository> _preferencesWriteRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly ChangePresenceStatusCommandHandler _handler;

    public ChangePresenceStatusCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _preferencesWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPresencePreferences entity, CancellationToken _) => entity);
        _preferencesWriteRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _preferencesWriteRepositoryMock
            .Setup(x => x.DeleteAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ChangePresenceStatusCommandHandler(
            _presenceStatusStoreMock.Object,
            _preferencesWriteRepositoryMock.Object,
            _mediatorMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
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
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task Handle_WithManualStatus_UpsertsPreference(PresenceStatus status)
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Active, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, status),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _preferencesWriteRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<UserPresencePreferences>(
                    preferences => preferences.UserId == userId && preferences.PreferredStatus == status),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _preferencesWriteRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithActiveStatus_DeletesPreference()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Busy, DateTimeOffset.UtcNow));
        _preferencesWriteRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserPresencePreferences.Create(userId, PresenceStatus.Busy));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Active),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _preferencesWriteRepositoryMock.Verify(
            x => x.DeleteAsync(
                It.Is<UserPresencePreferences>(preferences => preferences.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _preferencesWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFKStatus_DoesNotTouchPreference()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Active, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.AFK),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _preferencesWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _preferencesWriteRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()),
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

        var act = async () => await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Busy),
            CancellationToken.None);

        var result = await act();

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
    }
}

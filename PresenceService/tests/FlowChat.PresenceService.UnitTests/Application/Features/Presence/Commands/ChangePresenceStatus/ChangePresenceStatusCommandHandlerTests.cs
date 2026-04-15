using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class ChangePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionReadRepository> _readRepositoryMock = new();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IIntegrationEventPublisher> _integrationEventPublisherMock = new();
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

        _handler = new ChangePresenceStatusCommandHandler(
            _readRepositoryMock.Object,
            _presenceStatusStoreMock.Object,
            _integrationEventPublisherMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStatusChanges_StoresStatusAndPublishesDistinctRecipientEvent()
    {
        var userId = _fixture.Create<Guid>();
        var recipient1 = _fixture.Create<Guid>();
        var recipient2 = _fixture.Create<Guid>();
        var previous = new PresenceStatusSnapshot(userId, PresenceStatus.Active, DateTimeOffset.UtcNow.AddMinutes(-5));
        PresenceStatusChangedIntegrationEvent? capturedEvent = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([recipient1, recipient1, Guid.Empty, recipient2]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Busy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Key.Should().Be(userId.ToString("D"));
        capturedEvent.UserId.Should().Be(userId);
        capturedEvent.Status.Should().Be(PresenceStatus.Busy);
        capturedEvent.RecipientUserIds.Should().BeEquivalentTo([recipient1, recipient2]);
        capturedEvent.ChangedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Busy, capturedEvent.ChangedAtUtc, It.IsAny<CancellationToken>()),
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
        _readRepositoryMock.Verify(
            x => x.GetObserverUserIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(It.IsAny<Guid>(), It.IsAny<PresenceStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _integrationEventPublisherMock.Verify(
            x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCommitFails_RestoresPreviousStatus()
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = new PresenceStatusSnapshot(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-10));

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousStatus);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fixture.Create<Guid>()]);
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(async (operation, ct) =>
            {
                await operation(ct);
                throw new InvalidOperationException("commit failed");
            });

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
    public async Task Handle_WhenCommitFailsWithoutPreviousStatus_DeletesRedisEntry()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fixture.Create<Guid>()]);
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(async (operation, ct) =>
            {
                await operation(ct);
                throw new InvalidOperationException("commit failed");
            });

        var result = await _handler.Handle(
            new ChangePresenceStatusCommand(userId, PresenceStatus.Active),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
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
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fixture.Create<Guid>()]);
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(async (operation, ct) =>
            {
                await operation(ct);
                throw new InvalidOperationException("commit failed");
            });
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

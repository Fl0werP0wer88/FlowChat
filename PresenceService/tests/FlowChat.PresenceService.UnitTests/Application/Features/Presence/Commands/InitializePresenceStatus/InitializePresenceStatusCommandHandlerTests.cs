using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.InitializePresenceStatus;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class InitializePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionReadRepository> _readRepositoryMock = new();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IOutboxIntegrationEventPublisher> _integrationEventPublisherMock = new();
    private readonly Mock<IUserPresencePreferencesRepository> _preferencesRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly InitializePresenceStatusCommandHandler _handler;

    public InitializePresenceStatusCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InitializePresenceStatusCommandHandler(
            _readRepositoryMock.Object,
            _presenceStatusStoreMock.Object,
            _integrationEventPublisherMock.Object,
            _preferencesRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPresenceExists_ReturnsSuccessWithoutOverwriting()
    {
        var userId = _fixture.Create<Guid>();
        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Busy, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(It.IsAny<Guid>(), It.IsAny<PresenceStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _integrationEventPublisherMock.Verify(
            x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPresenceMissing_CreatesActiveStatusAndPublishesEvent()
    {
        var userId = _fixture.Create<Guid>();
        var observerUserId = _fixture.Create<Guid>();
        PresenceStatusChangedIntegrationEvent? capturedEvent = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([observerUserId, Guid.Empty, observerUserId]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be(userId);
        capturedEvent.Status.Should().Be(PresenceStatus.Active);
        capturedEvent.RecipientUserIds.Should().BeEquivalentTo([observerUserId]);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Active, capturedEvent.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPublishFails_RollsBackCreatedStatusAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("outbox failure"));

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorMessage.Should().Be("Failed to initialize presence status.");
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Active, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task Handle_WhenPreferenceExists_RestoresPreferredStatus(PresenceStatus preferredStatus)
    {
        var userId = _fixture.Create<Guid>();
        PresenceStatusChangedIntegrationEvent? capturedEvent = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _preferencesRepositoryMock
            .Setup(x => x.FindPreferredStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferredStatus);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Status.Should().Be(preferredStatus);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, preferredStatus, capturedEvent.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoPreference_UsesActiveStatus()
    {
        var userId = _fixture.Create<Guid>();
        PresenceStatusChangedIntegrationEvent? capturedEvent = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _preferencesRepositoryMock
            .Setup(x => x.FindPreferredStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatus?)null);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEvent!.Status.Should().Be(PresenceStatus.Active);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Active, capturedEvent.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

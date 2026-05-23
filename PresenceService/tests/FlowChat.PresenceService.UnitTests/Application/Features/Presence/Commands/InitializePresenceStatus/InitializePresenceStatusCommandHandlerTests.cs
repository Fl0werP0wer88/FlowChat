using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.InitializePresenceStatus;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class InitializePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IUserPresencePreferencesReadRepository> _preferencesReadRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly InitializePresenceStatusCommandHandler _handler;

    public InitializePresenceStatusCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<Func<FlowChatResult<Unit>, CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<Unit>>>,
                Func<FlowChatResult<Unit>, CancellationToken, Task<FlowChatResult<Unit>>>,
                CancellationToken>(async (operation, beforeCommitOperation, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InitializePresenceStatusCommandHandler(
            _presenceStatusStoreMock.Object,
            _preferencesReadRepositoryMock.Object,
            _mediatorMock.Object,
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
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPresenceMissing_CreatesActiveStatusAndPublishesApplicationEvent()
    {
        var userId = _fixture.Create<Guid>();
        PresenceStatusChangedApplicationEvent? capturedNotification = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedApplicationEvent, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.UserId.Should().Be(userId);
        capturedNotification.Status.Should().Be(PresenceStatus.Active);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Active, capturedNotification.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApplicationEventPublishFails_RollsBackCreatedStatusAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

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
        PresenceStatusChangedApplicationEvent? capturedNotification = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _preferencesReadRepositoryMock
            .Setup(x => x.FindPreferredStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferredStatus);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedApplicationEvent, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(preferredStatus);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, preferredStatus, capturedNotification.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoPreference_UsesActiveStatus()
    {
        var userId = _fixture.Create<Guid>();
        PresenceStatusChangedApplicationEvent? capturedNotification = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);
        _preferencesReadRepositoryMock
            .Setup(x => x.FindPreferredStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatus?)null);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedApplicationEvent, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new InitializePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(PresenceStatus.Active);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, PresenceStatus.Active, capturedNotification.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}


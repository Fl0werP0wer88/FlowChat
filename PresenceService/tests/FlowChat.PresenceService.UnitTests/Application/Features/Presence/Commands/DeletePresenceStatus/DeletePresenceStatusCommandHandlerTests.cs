using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class DeletePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly DeletePresenceStatusCommandHandler _handler;

    public DeletePresenceStatusCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new DeletePresenceStatusCommandHandler(
            _presenceStatusStoreMock.Object,
            _mediatorMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPresenceMissing_ReturnsSuccessWithoutDeletingOrPublishing()
    {
        var userId = _fixture.Create<Guid>();
        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceStatusSnapshot?)null);

        var result = await _handler.Handle(
            new DeletePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPresenceExists_DeletesPresenceAndPublishesInvisibleApplicationEvent()
    {
        var userId = _fixture.Create<Guid>();
        PresenceStatusChangedApplicationEvent? capturedNotification = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Busy, DateTimeOffset.UtcNow.AddMinutes(-10)));
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedApplicationEvent, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new DeletePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        capturedNotification.Should().NotBeNull();
        capturedNotification!.UserId.Should().Be(userId);
        capturedNotification.Status.Should().Be(PresenceStatus.Invisible);
    }

    [Fact]
    public async Task Handle_WhenApplicationEventPublishFails_RestoresPreviousStatusAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = new PresenceStatusSnapshot(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-5));

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousStatus);
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedApplicationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

        var result = await _handler.Handle(
            new DeletePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorMessage.Should().Be("Failed to delete presence status.");
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, previousStatus.Status, previousStatus.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}


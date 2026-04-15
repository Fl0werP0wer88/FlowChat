using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class DeletePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionReadRepository> _readRepositoryMock = new();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IIntegrationEventPublisher> _integrationEventPublisherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly DeletePresenceStatusCommandHandler _handler;

    public DeletePresenceStatusCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new DeletePresenceStatusCommandHandler(
            _readRepositoryMock.Object,
            _presenceStatusStoreMock.Object,
            _integrationEventPublisherMock.Object,
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
        _integrationEventPublisherMock.Verify(
            x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPresenceExists_DeletesPresenceAndPublishesInvisibleEvent()
    {
        var userId = _fixture.Create<Guid>();
        var observerUserId = _fixture.Create<Guid>();
        PresenceStatusChangedIntegrationEvent? capturedEvent = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, PresenceStatus.Busy, DateTimeOffset.UtcNow.AddMinutes(-10)));
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([observerUserId, Guid.Empty, observerUserId]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<PresenceStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusChangedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new DeletePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be(userId);
        capturedEvent.Status.Should().Be(PresenceStatus.Invisible);
        capturedEvent.RecipientUserIds.Should().BeEquivalentTo([observerUserId]);
    }
}

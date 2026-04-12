using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Infrastructure.Presence;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class PresenceStatusUpdateServiceTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IIntegrationEventPublisher> _integrationEventPublisherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task UpdateAndPublishAsync_WhenTransactionSucceeds_PersistsStatusAndPublishesEvent()
    {
        var userId = _fixture.Create<Guid>();
        var integrationEvent = CreateEvent(userId, PresenceStatus.AFK);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        var service = CreateService();

        var result = await service.UpdateAndPublishAsync(userId, null, integrationEvent, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, integrationEvent.Status, integrationEvent.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
        _integrationEventPublisherMock.Verify(
            x => x.PublishToOutboxAsync(integrationEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAndPublishAsync_WhenCommitFails_RestoresPreviousStatus()
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = new PresenceStatusSnapshot(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-10));
        var integrationEvent = CreateEvent(userId, PresenceStatus.Busy);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(async (operation, ct) =>
            {
                await operation(ct);
                throw new InvalidOperationException("commit failed");
            });

        var service = CreateService();

        var result = await service.UpdateAndPublishAsync(userId, previousStatus, integrationEvent, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, integrationEvent.Status, integrationEvent.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
        _presenceStatusStoreMock.Verify(
            x => x.SetAsync(userId, previousStatus.Status, previousStatus.ChangedAtUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAndPublishAsync_WhenCommitFailsWithoutPreviousStatus_DeletesRedisEntry()
    {
        var userId = _fixture.Create<Guid>();
        var integrationEvent = CreateEvent(userId, PresenceStatus.Invisible);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(async (operation, ct) =>
            {
                await operation(ct);
                throw new InvalidOperationException("commit failed");
            });

        var service = CreateService();

        var result = await service.UpdateAndPublishAsync(userId, null, integrationEvent, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAndPublishAsync_WhenRollbackFails_ReturnsFailureInsteadOfThrowing()
    {
        var userId = _fixture.Create<Guid>();
        var previousStatus = new PresenceStatusSnapshot(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-3));
        var integrationEvent = CreateEvent(userId, PresenceStatus.Busy);

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

        var service = CreateService();

        var act = async () => await service.UpdateAndPublishAsync(userId, previousStatus, integrationEvent, CancellationToken.None);

        var result = await act();

        result.IsFailure.Should().BeTrue();
    }

    private PresenceStatusUpdateService CreateService() =>
        new(
            _presenceStatusStoreMock.Object,
            _integrationEventPublisherMock.Object,
            _unitOfWorkMock.Object);

    private PresenceStatusChangedIntegrationEvent CreateEvent(Guid userId, PresenceStatus status) =>
        new()
        {
            Key = userId.ToString("D"),
            UserId = userId,
            Status = status,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            RecipientUserIds = [_fixture.Create<Guid>()]
        };
}

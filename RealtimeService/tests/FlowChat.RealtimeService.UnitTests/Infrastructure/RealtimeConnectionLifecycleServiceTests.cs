using AutoFixture;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeConnectionLifecycleServiceTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeConnectionRegistry> _registryMock = new();
    private readonly RecordingIntegrationEventPublisher _eventPublisher = new();
    private readonly RealtimeConnectionLifecycleService _service;

    public RealtimeConnectionLifecycleServiceTests()
    {
        _service = new RealtimeConnectionLifecycleService(
            _registryMock.Object,
            _eventPublisher,
            NullLogger<RealtimeConnectionLifecycleService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_WhenFirstConnection_PublishesRegisteredEvent()
    {
        var userId = _fixture.Create<Guid>();
        var mutation = new RealtimeConnectionMutationResult(userId, "connection-1", 1, DateTimeOffset.UtcNow);

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutation);

        await _service.RegisterAsync(userId, "connection-1", CancellationToken.None);

        var integrationEvent = _eventPublisher.Published.Should().ContainSingle().Subject
            .Should().BeOfType<RealtimeConnectionRegisteredIntegrationEvent>().Subject;
        integrationEvent.UserId.Should().Be(userId);
        integrationEvent.ConnectionId.Should().Be("connection-1");
        integrationEvent.ActiveConnectionCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_WhenAdditionalConnection_PublishesCurrentCount()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-2", 3, DateTimeOffset.UtcNow));

        await _service.RegisterAsync(userId, "connection-2", CancellationToken.None);

        _eventPublisher.Published.Should().ContainSingle()
            .Which.Should().BeOfType<RealtimeConnectionRegisteredIntegrationEvent>()
            .Which.ActiveConnectionCount.Should().Be(3);
    }

    [Fact]
    public async Task RegisterAsync_WhenPublishFails_CompensatesByUnregisteringAndThrows()
    {
        var userId = _fixture.Create<Guid>();
        _eventPublisher.PublishException = new InvalidOperationException("kafka unavailable");

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-3", 1, DateTimeOffset.UtcNow));
        _registryMock
            .Setup(x => x.UnregisterAsync("connection-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RealtimeConnectionMutationResult?)null);

        var act = () => _service.RegisterAsync(userId, "connection-3", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _registryMock.Verify(
            x => x.UnregisterAsync("connection-3", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterAsync_WhenLastConnection_PublishesUnregisteredEvent()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-4", 0, DateTimeOffset.UtcNow));

        await _service.UnregisterAsync("connection-4", CancellationToken.None);

        _eventPublisher.Published.Should().ContainSingle()
            .Which.Should().BeOfType<RealtimeConnectionUnregisteredIntegrationEvent>()
            .Which.ActiveConnectionCount.Should().Be(0);
    }

    [Fact]
    public async Task UnregisterAsync_WhenConnectionMissing_DoesNotPublish()
    {
        _registryMock
            .Setup(x => x.UnregisterAsync("missing-connection", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RealtimeConnectionMutationResult?)null);

        await _service.UnregisterAsync("missing-connection", CancellationToken.None);

        _eventPublisher.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task UnregisterAsync_WhenPublishFails_LogsBestEffortAndDoesNotThrow()
    {
        var userId = _fixture.Create<Guid>();
        _eventPublisher.PublishException = new InvalidOperationException("kafka unavailable");

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-5", 0, DateTimeOffset.UtcNow));

        var act = () => _service.UnregisterAsync("connection-5", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

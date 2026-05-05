using AutoFixture;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeConnectionCommandOrchestratorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeConnectionRegistry> _registryMock = new();
    private readonly RecordingIntegrationEventPublisher _eventPublisher = new();
    private readonly RealtimeConnectionCommandOrchestrator _orchestrator;

    public RealtimeConnectionCommandOrchestratorTests()
    {
        _orchestrator = new RealtimeConnectionCommandOrchestrator(
            _registryMock.Object,
            _eventPublisher,
            NullLogger<RealtimeConnectionCommandOrchestrator>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_WhenFirstConnection_PublishesRegisteredEvent()
    {
        var userId = _fixture.Create<Guid>();
        var mutation = new RealtimeConnectionMutationResult(userId, "connection-1", 1, true, false, DateTimeOffset.UtcNow);

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutation);

        var result = await _orchestrator.RegisterAsync(userId, "connection-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integrationEvent = _eventPublisher.Published.Should().ContainSingle().Subject
            .Should().BeOfType<RealtimeConnectionRegisteredIntegrationEvent>().Subject;
        integrationEvent.UserId.Should().Be(userId);
        integrationEvent.ConnectionId.Should().Be("connection-1");
        integrationEvent.ActiveConnectionCount.Should().Be(1);
        integrationEvent.IsFirstConnectionForUser.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WhenAdditionalConnection_PublishesCurrentCountAndFirstConnectionFlag()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-2", 3, false, false, DateTimeOffset.UtcNow));

        var result = await _orchestrator.RegisterAsync(userId, "connection-2", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integrationEvent = _eventPublisher.Published.Should().ContainSingle().Subject
            .Should().BeOfType<RealtimeConnectionRegisteredIntegrationEvent>().Subject;
        integrationEvent.ActiveConnectionCount.Should().Be(3);
        integrationEvent.IsFirstConnectionForUser.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_WhenPublishFails_CompensatesByUnregisteringAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        _eventPublisher.PublishException = new InvalidOperationException("kafka unavailable");

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-3", 1, true, false, DateTimeOffset.UtcNow));
        _registryMock
            .Setup(x => x.UnregisterAsync("connection-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RealtimeConnectionMutationResult?)null);

        var result = await _orchestrator.RegisterAsync(userId, "connection-3", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _registryMock.Verify(
            x => x.UnregisterAsync("connection-3", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenCanceled_CompensatesByUnregisteringAndRethrows()
    {
        var userId = _fixture.Create<Guid>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-canceled", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));
        _registryMock
            .Setup(x => x.UnregisterAsync("connection-canceled", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RealtimeConnectionMutationResult?)null);

        var act = () => _orchestrator.RegisterAsync(userId, "connection-canceled", cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _registryMock.Verify(
            x => x.UnregisterAsync("connection-canceled", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterAsync_WhenLastConnection_PublishesUnregisteredEvent()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-4", 0, false, true, DateTimeOffset.UtcNow));

        var result = await _orchestrator.UnregisterAsync("connection-4", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integrationEvent = _eventPublisher.Published.Should().ContainSingle().Subject
            .Should().BeOfType<RealtimeConnectionUnregisteredIntegrationEvent>().Subject;
        integrationEvent.ActiveConnectionCount.Should().Be(0);
        integrationEvent.IsLastConnectionForUser.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisterAsync_WhenActiveConnectionsRemain_PublishesCurrentCountAndLastConnectionFlag()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-remains", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-remains", 1, false, false, DateTimeOffset.UtcNow));

        var result = await _orchestrator.UnregisterAsync("connection-remains", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integrationEvent = _eventPublisher.Published.Should().ContainSingle().Subject
            .Should().BeOfType<RealtimeConnectionUnregisteredIntegrationEvent>().Subject;
        integrationEvent.ActiveConnectionCount.Should().Be(1);
        integrationEvent.IsLastConnectionForUser.Should().BeFalse();
    }

    [Fact]
    public async Task UnregisterAsync_WhenConnectionMissing_DoesNotPublish()
    {
        _registryMock
            .Setup(x => x.UnregisterAsync("missing-connection", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RealtimeConnectionMutationResult?)null);

        var result = await _orchestrator.UnregisterAsync("missing-connection", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _eventPublisher.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task UnregisterAsync_WhenPublishFails_ReturnsSuccess()
    {
        var userId = _fixture.Create<Guid>();
        _eventPublisher.PublishException = new InvalidOperationException("kafka unavailable");

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-5", 0, false, true, DateTimeOffset.UtcNow));

        var result = await _orchestrator.UnregisterAsync("connection-5", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

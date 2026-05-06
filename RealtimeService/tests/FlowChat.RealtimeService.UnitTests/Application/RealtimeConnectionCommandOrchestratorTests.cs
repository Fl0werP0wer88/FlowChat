using AutoFixture;
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
    private readonly Mock<IPresenceInternalApiClient> _presenceInternalApiClientMock = new();
    private readonly RealtimeConnectionCommandOrchestrator _orchestrator;

    public RealtimeConnectionCommandOrchestratorTests()
    {
        _orchestrator = new RealtimeConnectionCommandOrchestrator(
            _registryMock.Object,
            _presenceInternalApiClientMock.Object,
            NullLogger<RealtimeConnectionCommandOrchestrator>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_WhenConnectionRegistered_ReturnsSuccessWithoutPublishingEvent()
    {
        var userId = _fixture.Create<Guid>();
        var mutation = new RealtimeConnectionMutationResult(userId, "connection-1", 1, true, false, DateTimeOffset.UtcNow);

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutation);

        var result = await _orchestrator.RegisterAsync(userId, "connection-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClientMock.Verify(
            x => x.DeletePresenceStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenRegistryFails_CompensatesByUnregisteringAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.RegisterAsync(userId, "connection-3", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis unavailable"));
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
    public async Task UnregisterAsync_WhenLastConnection_DeletesPresenceStatus()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-4", 0, false, true, DateTimeOffset.UtcNow));

        var result = await _orchestrator.UnregisterAsync("connection-4", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClientMock.Verify(
            x => x.DeletePresenceStatusAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterAsync_WhenActiveConnectionsRemain_DoesNotDeletePresenceStatus()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-remains", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-remains", 1, false, false, DateTimeOffset.UtcNow));

        var result = await _orchestrator.UnregisterAsync("connection-remains", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClientMock.Verify(
            x => x.DeletePresenceStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnregisterAsync_WhenConnectionMissing_DoesNotDeletePresenceStatus()
    {
        _registryMock
            .Setup(x => x.UnregisterAsync("missing-connection", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RealtimeConnectionMutationResult?)null);

        var result = await _orchestrator.UnregisterAsync("missing-connection", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClientMock.Verify(
            x => x.DeletePresenceStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnregisterAsync_WhenPresenceDeleteFails_ReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();

        _registryMock
            .Setup(x => x.UnregisterAsync("connection-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RealtimeConnectionMutationResult(userId, "connection-5", 0, false, true, DateTimeOffset.UtcNow));
        _presenceInternalApiClientMock
            .Setup(x => x.DeletePresenceStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("PresenceService unavailable"));

        var result = await _orchestrator.UnregisterAsync("connection-5", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
    }
}

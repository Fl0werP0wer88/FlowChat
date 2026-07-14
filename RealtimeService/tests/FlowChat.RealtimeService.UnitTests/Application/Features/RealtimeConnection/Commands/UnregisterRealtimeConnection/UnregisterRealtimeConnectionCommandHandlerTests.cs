using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class UnregisterRealtimeConnectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly CapturingRealtimeConnectionRegistry _registry = new();
    private readonly CapturingPresenceInternalApiClient _presenceInternalApiClient = new();
    private readonly UnregisterRealtimeConnectionCommandHandler _handler;

    public UnregisterRealtimeConnectionCommandHandlerTests()
    {
        _handler = new UnregisterRealtimeConnectionCommandHandler(
            _registry,
            _presenceInternalApiClient,
            NullLogger<UnregisterRealtimeConnectionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenLastConnection_DeletesPresenceStatus()
    {
        var userId = _fixture.Create<Guid>();
        _registry.UnregisterResult = new RealtimeConnectionMutationResult(userId, "connection-4", 0, false, true, DateTimeOffset.UtcNow);

        var result = await _handler.Handle(
            new UnregisterRealtimeConnectionCommand("connection-4"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClient.LastDeletePresenceStatusUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenActiveConnectionsRemain_DoesNotDeletePresenceStatus()
    {
        var userId = _fixture.Create<Guid>();
        _registry.UnregisterResult = new RealtimeConnectionMutationResult(userId, "connection-remains", 1, false, false, DateTimeOffset.UtcNow);

        var result = await _handler.Handle(
            new UnregisterRealtimeConnectionCommand("connection-remains"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClient.LastDeletePresenceStatusUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenConnectionMissing_DoesNotDeletePresenceStatus()
    {
        _registry.UnregisterResult = null;

        var result = await _handler.Handle(
            new UnregisterRealtimeConnectionCommand("missing-connection"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceInternalApiClient.LastDeletePresenceStatusUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPresenceDeleteFails_ReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        _registry.UnregisterResult = new RealtimeConnectionMutationResult(userId, "connection-5", 0, false, true, DateTimeOffset.UtcNow);
        _presenceInternalApiClient.DeletePresenceStatusException = new HttpRequestException("PresenceService unavailable");

        var result = await _handler.Handle(
            new UnregisterRealtimeConnectionCommand("connection-5"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
    }
}

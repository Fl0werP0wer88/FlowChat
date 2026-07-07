using AutoFixture;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RegisterRealtimeConnectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly CapturingRealtimeConnectionRegistry _registry = new();
    private readonly RegisterRealtimeConnectionCommandHandler _handler;

    public RegisterRealtimeConnectionCommandHandlerTests()
    {
        _handler = new RegisterRealtimeConnectionCommandHandler(
            _registry,
            NullLogger<RegisterRealtimeConnectionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenConnectionRegistered_ReturnsSuccess()
    {
        var userId = _fixture.Create<Guid>();

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _registry.LastRegisteredUserId.Should().Be(userId);
        _registry.LastRegisteredConnectionId.Should().Be("connection-1");
    }

    [Fact]
    public async Task Handle_WhenRegistryFails_CompensatesByUnregisteringAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        _registry.RegisterException = new InvalidOperationException("redis unavailable");

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-3"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _registry.LastUnregisteredConnectionId.Should().Be("connection-3");
    }

    [Fact]
    public async Task Handle_WhenCanceled_CompensatesByUnregisteringAndRethrows()
    {
        var userId = _fixture.Create<Guid>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        _registry.RegisterException = new OperationCanceledException(cancellationTokenSource.Token);

        var act = () => _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-canceled"),
            cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _registry.LastUnregisteredConnectionId.Should().Be("connection-canceled");
    }
}

using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeConnectionCommandHandlerTests
{
    [Fact]
    public async Task RegisterHandler_WhenUserIdMissing_ReturnsBadRequest()
    {
        var orchestrator = new FakeRealtimeConnectionCommandOrchestrator();
        var handler = new RegisterRealtimeConnectionCommandHandler(orchestrator);

        var result = await handler.Handle(
            new RegisterRealtimeConnectionCommand(Guid.Empty, "connection-1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        orchestrator.LastRegisteredUserId.Should().BeNull();
    }

    [Fact]
    public async Task RegisterHandler_WhenRequestValid_DelegatesToOrchestrator()
    {
        var userId = Guid.NewGuid();
        var orchestrator = new FakeRealtimeConnectionCommandOrchestrator();
        var handler = new RegisterRealtimeConnectionCommandHandler(orchestrator);

        var result = await handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        orchestrator.LastRegisteredUserId.Should().Be(userId);
        orchestrator.LastRegisteredConnectionId.Should().Be("connection-1");
    }

    [Fact]
    public async Task UnregisterHandler_WhenConnectionIdMissing_ReturnsBadRequest()
    {
        var orchestrator = new FakeRealtimeConnectionCommandOrchestrator();
        var handler = new UnregisterRealtimeConnectionCommandHandler(orchestrator);

        var result = await handler.Handle(
            new UnregisterRealtimeConnectionCommand(" "),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        orchestrator.LastUnregisteredConnectionId.Should().BeNull();
    }

    [Fact]
    public async Task UnregisterHandler_WhenRequestValid_DelegatesToOrchestrator()
    {
        var orchestrator = new FakeRealtimeConnectionCommandOrchestrator();
        var handler = new UnregisterRealtimeConnectionCommandHandler(orchestrator);

        var result = await handler.Handle(
            new UnregisterRealtimeConnectionCommand("connection-9"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        orchestrator.LastUnregisteredConnectionId.Should().Be("connection-9");
    }

    private sealed class FakeRealtimeConnectionCommandOrchestrator : IRealtimeConnectionCommandOrchestrator
    {
        public Guid? LastRegisteredUserId { get; private set; }
        public string? LastRegisteredConnectionId { get; private set; }
        public string? LastUnregisteredConnectionId { get; private set; }

        public Task<FlowChatResult<Unit>> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
        {
            LastRegisteredUserId = userId;
            LastRegisteredConnectionId = connectionId;
            return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
        }

        public Task<FlowChatResult<Unit>> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
        {
            LastUnregisteredConnectionId = connectionId;
            return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
        }
    }
}

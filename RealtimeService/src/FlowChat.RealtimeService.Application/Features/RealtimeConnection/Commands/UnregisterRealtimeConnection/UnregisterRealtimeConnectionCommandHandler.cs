using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;

public sealed class UnregisterRealtimeConnectionCommandHandler(IRealtimeConnectionCommandOrchestrator orchestrator)
    : ICommandHandler<UnregisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeConnectionCommandOrchestrator _orchestrator = orchestrator
        ?? throw new ArgumentNullException(nameof(orchestrator));

    public Task<FlowChatResult<Unit>> Handle(UnregisterRealtimeConnectionCommand request, CancellationToken cancellationToken) =>
        _orchestrator.UnregisterAsync(request.ConnectionId!, cancellationToken);
}

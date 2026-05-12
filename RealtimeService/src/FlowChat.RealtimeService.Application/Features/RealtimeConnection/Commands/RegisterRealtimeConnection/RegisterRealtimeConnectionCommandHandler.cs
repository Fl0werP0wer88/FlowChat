using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

public sealed class RegisterRealtimeConnectionCommandHandler(IRealtimeConnectionCommandOrchestrator orchestrator)
    : ICommandHandler<RegisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeConnectionCommandOrchestrator _orchestrator = orchestrator
        ?? throw new ArgumentNullException(nameof(orchestrator));

    public Task<FlowChatResult<Unit>> Handle(RegisterRealtimeConnectionCommand request, CancellationToken cancellationToken) =>
        _orchestrator.RegisterAsync(request.UserId, request.ConnectionId!, cancellationToken);
}

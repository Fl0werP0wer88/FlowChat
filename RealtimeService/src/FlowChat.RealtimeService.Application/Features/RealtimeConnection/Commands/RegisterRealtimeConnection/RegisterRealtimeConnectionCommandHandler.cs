using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

public sealed class RegisterRealtimeConnectionCommandHandler(IRealtimeConnectionCommandOrchestrator orchestrator)
    : ICommandHandler<RegisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeConnectionCommandOrchestrator _orchestrator = orchestrator
        ?? throw new ArgumentNullException(nameof(orchestrator));

    public Task<FlowChatResult<Unit>> Handle(RegisterRealtimeConnectionCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Task.FromResult(FlowChatResult<Unit>.Failure(DomainError.BadRequest("UserId is required.")));
        }

        if (string.IsNullOrWhiteSpace(request.ConnectionId))
        {
            return Task.FromResult(FlowChatResult<Unit>.Failure(DomainError.BadRequest("ConnectionId is required.")));
        }

        return _orchestrator.RegisterAsync(request.UserId, request.ConnectionId, cancellationToken);
    }
}

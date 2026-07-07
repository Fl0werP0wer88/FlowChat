using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

// Logs structured failure context (user/connection ids) and the best-effort compensation warning that LoggingPipelineBehaviour cannot see
public sealed class RegisterRealtimeConnectionCommandHandler(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    ILogger<RegisterRealtimeConnectionCommandHandler> logger)
    : ICommandHandler<RegisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly ILogger<RegisterRealtimeConnectionCommandHandler> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FlowChatResult<Unit>> Handle(RegisterRealtimeConnectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _realtimeConnectionRegistry.RegisterAsync(request.UserId, request.ConnectionId!, cancellationToken);

            return FlowChatResult<Unit>.Success(Unit.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryCompensateRegistrationAsync(request.ConnectionId!);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to register realtime connection for user {UserId} and connection {ConnectionId}.",
                request.UserId,
                request.ConnectionId);

            await TryCompensateRegistrationAsync(request.ConnectionId!);

            return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."));
        }
    }

    private async Task TryCompensateRegistrationAsync(string connectionId)
    {
        try
        {
            await _realtimeConnectionRegistry.UnregisterAsync(connectionId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to compensate realtime connection registration for connection {ConnectionId} after publish failure.",
                connectionId);
        }
    }
}

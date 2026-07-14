using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;

// Logs structured failure context when deleting the presence status fails, which LoggingPipelineBehaviour cannot see
public sealed class UnregisterRealtimeConnectionCommandHandler(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<UnregisterRealtimeConnectionCommandHandler> logger)
    : ICommandHandler<UnregisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IPresenceInternalApiClient _presenceInternalApiClient = presenceInternalApiClient
        ?? throw new ArgumentNullException(nameof(presenceInternalApiClient));
    private readonly ILogger<UnregisterRealtimeConnectionCommandHandler> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FlowChatResult<Unit>> Handle(UnregisterRealtimeConnectionCommand request, CancellationToken cancellationToken)
    {
        var mutation = await _realtimeConnectionRegistry.UnregisterAsync(request.ConnectionId!, cancellationToken);
        if (mutation is null)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        if (!mutation.IsLastConnectionForUser)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        try
        {
            await _presenceInternalApiClient.DeletePresenceStatusAsync(mutation.UserId, cancellationToken);
            return FlowChatResult<Unit>.Success(Unit.Value);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete presence status for user {UserId} after unregistering realtime connection {ConnectionId}.",
                mutation.UserId,
                mutation.ConnectionId);

            return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to delete presence status."));
        }
    }
}

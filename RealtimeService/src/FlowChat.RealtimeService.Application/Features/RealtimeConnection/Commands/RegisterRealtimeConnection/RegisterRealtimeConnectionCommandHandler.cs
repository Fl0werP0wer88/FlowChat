using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

// Logs structured failure context (user/connection ids) that LoggingPipelineBehaviour cannot see
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
            await _realtimeConnectionRegistry.UnregisterAsync(request.ConnectionId!, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to register realtime connection for user {UserId} and connection {ConnectionId}.",
                request.UserId,
                request.ConnectionId);

            await _realtimeConnectionRegistry.UnregisterAsync(request.ConnectionId!, CancellationToken.None);

            return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."));
        }
    }
}

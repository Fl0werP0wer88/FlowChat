using CSharpFunctionalExtensions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;

public interface IRealtimeConnectionCommandOrchestrator
{
    Task<FlowChatResult<Unit>> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken);

    Task<FlowChatResult<Unit>> UnregisterAsync(string connectionId, CancellationToken cancellationToken);
}

internal sealed class RealtimeConnectionCommandOrchestrator(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IDirectEventPublisher integrationEventPublisher,
    ILogger<RealtimeConnectionCommandOrchestrator> logger) : IRealtimeConnectionCommandOrchestrator
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IDirectEventPublisher _integrationEventPublisher = integrationEventPublisher
        ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    private readonly ILogger<RealtimeConnectionCommandOrchestrator> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FlowChatResult<Unit>> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var mutation = await _realtimeConnectionRegistry.RegisterAsync(userId, connectionId, cancellationToken);

            await _integrationEventPublisher.Publish(
                new IntegrationEventEnvelope<RealtimeConnectionRegisteredIntegrationEvent>(
                    new RealtimeConnectionRegisteredIntegrationEvent
                    {
                        UserId = mutation.UserId,
                        ConnectionId = mutation.ConnectionId,
                        ActiveConnectionCount = mutation.ActiveConnectionCount,
                        IsFirstConnectionForUser = mutation.IsFirstConnectionForUser,
                        OccurredAtUtc = mutation.OccurredAtUtc
                    },
                    mutation.UserId.ToString("D")),
                cancellationToken);

            return FlowChatResult<Unit>.Success(Unit.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryCompensateRegistrationAsync(connectionId);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to register realtime connection for user {UserId} and connection {ConnectionId}.",
                userId,
                connectionId);

            await TryCompensateRegistrationAsync(connectionId);

            return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."));
        }
    }

    public async Task<FlowChatResult<Unit>> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        var mutation = await _realtimeConnectionRegistry.UnregisterAsync(connectionId, cancellationToken);
        if (mutation is null)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        try
        {
            await _integrationEventPublisher.Publish(
                new IntegrationEventEnvelope<RealtimeConnectionUnregisteredIntegrationEvent>(
                    new RealtimeConnectionUnregisteredIntegrationEvent
                    {
                        UserId = mutation.UserId,
                        ConnectionId = mutation.ConnectionId,
                        ActiveConnectionCount = mutation.ActiveConnectionCount,
                        IsLastConnectionForUser = mutation.IsLastConnectionForUser,
                        OccurredAtUtc = mutation.OccurredAtUtc
                    },
                    mutation.UserId.ToString("D")),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish realtime connection unregistered event for user {UserId} and connection {ConnectionId}.",
                mutation.UserId,
                mutation.ConnectionId);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
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

using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

public sealed class RealtimeConnectionLifecycleService(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<RealtimeConnectionLifecycleService> logger) : IRealtimeConnectionLifecycleService
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher
        ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    private readonly ILogger<RealtimeConnectionLifecycleService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        var mutation = await _realtimeConnectionRegistry.RegisterAsync(userId, connectionId, cancellationToken);

        try
        {
            await _integrationEventPublisher.PublishToOutboxAsync(
                new RealtimeConnectionRegisteredIntegrationEvent
                {
                    Key = mutation.UserId.ToString("D"),
                    UserId = mutation.UserId,
                    ConnectionId = mutation.ConnectionId,
                    ActiveConnectionCount = mutation.ActiveConnectionCount,
                    OccurredAtUtc = mutation.OccurredAtUtc
                },
                cancellationToken);
        }
        catch
        {
            await TryCompensateRegistrationAsync(connectionId);
            throw;
        }
    }

    public async Task UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        var mutation = await _realtimeConnectionRegistry.UnregisterAsync(connectionId, cancellationToken);
        if (mutation is null)
        {
            return;
        }

        try
        {
            await _integrationEventPublisher.PublishToOutboxAsync(
                new RealtimeConnectionUnregisteredIntegrationEvent
                {
                    Key = mutation.UserId.ToString("D"),
                    UserId = mutation.UserId,
                    ConnectionId = mutation.ConnectionId,
                    ActiveConnectionCount = mutation.ActiveConnectionCount,
                    OccurredAtUtc = mutation.OccurredAtUtc
                },
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

using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

// Keeps Redis connection metadata alive for locally active realtime clients and refreshes their presence lease upstream
internal sealed class RealtimeConnectionRefreshBackgroundService(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IActiveConnectionsTracker activeConnectionsTracker,
    IPresenceInternalApiClient presenceInternalApiClient,
    RealtimeConnectionsSettingsSection settings,
    ILogger<RealtimeConnectionRefreshBackgroundService> logger) : BackgroundService
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IActiveConnectionsTracker _activeConnectionsTracker = activeConnectionsTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionsTracker));
    private readonly IPresenceInternalApiClient _presenceInternalApiClient = presenceInternalApiClient
        ?? throw new ArgumentNullException(nameof(presenceInternalApiClient));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<RealtimeConnectionRefreshBackgroundService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_settings.RefreshInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Only locally tracked connections are refreshed here; other instances are responsible for extending their own TTLs
            var connections = _activeConnectionsTracker.Snapshot();
            if (connections.Count == 0)
            {
                continue;
            }

            try
            {
                await _realtimeConnectionRegistry.RefreshAsync(connections, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to refresh realtime connection TTLs in Redis.");
            }

            try
            {
                // Presence refresh is isolated from Redis refresh so a transient failure in one system does not stop the other
                var userIds = connections
                    .Select(static c => c.UserId)
                    .Where(static id => id != Guid.Empty)
                    .Distinct()
                    .ToArray();

                await _presenceInternalApiClient.RefreshPresenceStatusAsync(userIds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to refresh presence status TTLs in PresenceService.");
            }
        }
    }
}

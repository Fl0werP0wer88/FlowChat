using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RealtimeConnectionRefreshBackgroundService(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IActiveConnectionsTracker activeConnectionsTracker,
    RealtimeConnectionsSettings settings,
    ILogger<RealtimeConnectionRefreshBackgroundService> logger) : BackgroundService
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IActiveConnectionsTracker _activeConnectionsTracker = activeConnectionsTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionsTracker));
    private readonly RealtimeConnectionsSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<RealtimeConnectionRefreshBackgroundService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_settings.RefreshInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
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
        }
    }
}

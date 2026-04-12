using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RealtimeConnectionRefreshBackgroundService(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IActiveRealtimeConnectionTracker activeConnectionTracker,
    RealtimeConnectionsSettings settings,
    ILogger<RealtimeConnectionRefreshBackgroundService> logger) : BackgroundService
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IActiveRealtimeConnectionTracker _activeConnectionTracker = activeConnectionTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionTracker));
    private readonly RealtimeConnectionsSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<RealtimeConnectionRefreshBackgroundService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_settings.RefreshInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            var connectionIds = _activeConnectionTracker.Snapshot();
            if (connectionIds.Count == 0)
            {
                continue;
            }

            try
            {
                await _realtimeConnectionRegistry.RefreshAsync(connectionIds, stoppingToken);
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

namespace FlowChat.AuthService.OutboxPublisher.Diagnostics;

public sealed class OutboxWorkerStartupProbe(
    IAuthDbConnectivityProbe authDbConnectivityProbe,
    IKafkaConnectivityProbe kafkaConnectivityProbe,
    ILogger<OutboxWorkerStartupProbe> logger)
    : IHostedService
{
    private readonly IAuthDbConnectivityProbe _authDbConnectivityProbe = authDbConnectivityProbe;
    private readonly IKafkaConnectivityProbe _kafkaConnectivityProbe = kafkaConnectivityProbe;
    private readonly ILogger<OutboxWorkerStartupProbe> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running AuthService outbox publisher startup connectivity probes.");

        await RunProbeAsync("AuthDb", _authDbConnectivityProbe.ProbeAsync, cancellationToken);
        await RunProbeAsync("Kafka", _kafkaConnectivityProbe.ProbeAsync, cancellationToken);

        _logger.LogInformation("AuthService outbox publisher startup connectivity probes completed successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunProbeAsync(
        string dependencyName,
        Func<CancellationToken, Task> probe,
        CancellationToken cancellationToken)
    {
        try
        {
            await probe(cancellationToken);
            _logger.LogInformation("{DependencyName} connectivity probe succeeded.", dependencyName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "{DependencyName} connectivity probe failed. Outbox publisher startup will be aborted.",
                dependencyName);

            throw new InvalidOperationException(
                $"{dependencyName} connectivity probe failed. See logs and inner exception for details.",
                ex);
        }
    }
}

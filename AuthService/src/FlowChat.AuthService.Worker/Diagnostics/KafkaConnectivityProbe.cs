using Confluent.Kafka;
using FlowChat.AuthService.Infrastructure.Kafka;

namespace FlowChat.AuthService.Worker.Diagnostics;

public sealed class KafkaConnectivityProbe : IKafkaConnectivityProbe
{
    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(5);
    private readonly IWorkerSettingsManager _workerSettingsManager;

    public KafkaConnectivityProbe(IWorkerSettingsManager workerSettingsManager)
    {
        _workerSettingsManager = workerSettingsManager;
    }

    public Task ProbeAsync(CancellationToken cancellationToken)
    {
        var userCreatedOptions = _workerSettingsManager.GetUserCreatedProducerOptions();
        var emailVerificationOptions = _workerSettingsManager.GetUserEmailVerificationRequestedProducerOptions();
        var bootstrapServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddBootstrapServers(bootstrapServers, userCreatedOptions.BootstrapServers);
        AddBootstrapServers(bootstrapServers, emailVerificationOptions.BootstrapServers);

        foreach (var bootstrapServer in bootstrapServers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProbeBootstrapServers(bootstrapServer);
        }

        return Task.CompletedTask;
    }

    private static void AddBootstrapServers(ISet<string> bootstrapServers, string? configuredBootstrapServers)
    {
        if (!string.IsNullOrWhiteSpace(configuredBootstrapServers))
        {
            bootstrapServers.Add(configuredBootstrapServers);
        }
    }

    private static void ProbeBootstrapServers(string bootstrapServers)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrapServers,
                SocketTimeoutMs = (int)MetadataTimeout.TotalMilliseconds
            }).Build();

            var metadata = adminClient.GetMetadata(MetadataTimeout);
            if (metadata.Brokers.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Kafka bootstrap servers '{bootstrapServers}' returned no brokers.");
            }
        }
        catch (Exception ex) when (ex is KafkaException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Unable to connect to Kafka bootstrap servers '{bootstrapServers}'.",
                ex);
        }
    }
}

using Confluent.Kafka;
using FlowChat.AuthService.OutboxPublisher.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.OutboxPublisher.Diagnostics;

public sealed class KafkaConnectivityProbe(
    IOptions<AccountRegisteredProducerSettingsSection> accountRegisteredOptions,
    IOptions<AccountConfirmedProducerSettingsSection> accountConfirmedOptions,
    IOptions<PhoneNumberConfirmedProducerSettingsSection> phoneNumberConfirmedOptions,
    IOptions<RetryOutboxKafkaSettingsSection> retryOutboxOptions)
    : IKafkaConnectivityProbe
{
    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(5);
    private readonly AccountRegisteredProducerSettingsSection _accountRegisteredOptions = accountRegisteredOptions.Value;
    private readonly AccountConfirmedProducerSettingsSection _accountConfirmedOptions = accountConfirmedOptions.Value;
    private readonly PhoneNumberConfirmedProducerSettingsSection _phoneNumberConfirmedOptions = phoneNumberConfirmedOptions.Value;
    private readonly RetryOutboxKafkaSettingsSection _retryOutboxOptions = retryOutboxOptions.Value;

    public Task ProbeAsync(CancellationToken cancellationToken)
    {
        var bootstrapServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddBootstrapServers(bootstrapServers, _accountRegisteredOptions.BootstrapServers);
        AddBootstrapServers(bootstrapServers, _accountConfirmedOptions.BootstrapServers);
        AddBootstrapServers(bootstrapServers, _phoneNumberConfirmedOptions.BootstrapServers);
        AddBootstrapServers(bootstrapServers, _retryOutboxOptions.BootstrapServers);

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

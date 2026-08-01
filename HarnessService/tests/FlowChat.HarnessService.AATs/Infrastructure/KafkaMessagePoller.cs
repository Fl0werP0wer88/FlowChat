using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using FlowChat.HarnessService.Consumers.Kafka.Retry;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public static class KafkaMessagePoller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Task<ConsumedRetryPipelineMessage?> WaitForRetryMessageAsync(
        string bootstrapServers,
        string topic,
        Guid scenarioId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        Task.Run(
            () => WaitForRetryMessage(
                bootstrapServers,
                topic,
                scenarioId,
                timeout,
                cancellationToken),
            cancellationToken);

    private static ConsumedRetryPipelineMessage? WaitForRetryMessage(
        string bootstrapServers,
        string topic,
        Guid scenarioId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"harness-aat-observer-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
            if (result?.Message?.Value is null)
                continue;

            RetryPipelineTestIntegrationEvent? message;
            try
            {
                message = JsonSerializer.Deserialize<RetryPipelineTestIntegrationEvent>(
                    result.Message.Value,
                    JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (message?.ScenarioId != scenarioId)
                continue;

            var headers = result.Message.Headers
                .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => Encoding.UTF8.GetString(group.Last().GetValueBytes()),
                    StringComparer.OrdinalIgnoreCase);
            return new ConsumedRetryPipelineMessage(message, result.Message.Key, headers);
        }

        return null;
    }
}

public sealed record ConsumedRetryPipelineMessage(
    RetryPipelineTestIntegrationEvent Message,
    string KafkaKey,
    IReadOnlyDictionary<string, string> Headers);

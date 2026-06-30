using System.Text.Json;
using Confluent.Kafka;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Projections.Models;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public static class KafkaDlqPoller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Task<ProjectionIntegrationEvent<ProjectionTestReadModel>?> WaitForMessageAsync(
        string bootstrapServers,
        string topic,
        Guid sourceAggregateId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        Task.Run(
            () => WaitForMessage(bootstrapServers, topic, sourceAggregateId, timeout, cancellationToken),
            cancellationToken);

    private static ProjectionIntegrationEvent<ProjectionTestReadModel>? WaitForMessage(
        string bootstrapServers,
        string topic,
        Guid sourceAggregateId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"harness-aat-dlq-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(topic);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remaining = deadline - DateTimeOffset.UtcNow;
            var consumeTimeout = remaining < TimeSpan.FromMilliseconds(250)
                ? remaining
                : TimeSpan.FromMilliseconds(250);

            if (consumeTimeout <= TimeSpan.Zero)
                break;

            var result = consumer.Consume(consumeTimeout);
            if (result?.Message?.Value is null)
                continue;

            var message = TryDeserialize(result.Message.Value);
            if (message?.SourceAggregateId == sourceAggregateId)
                return message;
        }

        return null;
    }

    private static ProjectionIntegrationEvent<ProjectionTestReadModel>? TryDeserialize(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ProjectionIntegrationEvent<ProjectionTestReadModel>>(value, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

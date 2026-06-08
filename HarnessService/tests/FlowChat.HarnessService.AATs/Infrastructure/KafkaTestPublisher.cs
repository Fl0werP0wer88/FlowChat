using System.Text.Json;
using Confluent.Kafka;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Projections.Models;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class KafkaTestPublisher : IAsyncDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;

    public KafkaTestPublisher(string bootstrapServers, string topic)
    {
        _topic = topic;
        _producer = new ProducerBuilder<Null, string>(
            new ProducerConfig { BootstrapServers = bootstrapServers })
            .Build();
    }

    public async Task PublishAsync(
        Guid id,
        string payload,
        int version,
        OperationType operation = OperationType.Created,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var @event = new ProjectionIntegrationEvent<ProjectionTestReadModel>
        {
            SourceAggregateId = id,
            SourceAggregateCreatedAtUtc = now,
            SourceAggregateModifiedAtUtc = now,
            SourceAggregateDeletedAt = operation == OperationType.Deleted ? now : null,
            Value = new ProjectionTestReadModel { Payload = payload },
            Operation = operation,
            SourceAggregateVersion = version
        };

        var json = JsonSerializer.Serialize(@event);
        await _producer.ProduceAsync(
            _topic,
            new Message<Null, string> { Value = json },
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}

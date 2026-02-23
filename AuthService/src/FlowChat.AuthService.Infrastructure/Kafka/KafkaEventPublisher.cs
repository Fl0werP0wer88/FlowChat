using System.Text.Json;
using Confluent.Kafka;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public class KafkaEventPublisher<TEvent, TOptions>
    : IKafkaEventPublisher<TEvent>, IDisposable
    where TOptions : class, IKafkaProducerOptions<TEvent>
{
    private readonly TOptions _options;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger _logger;

    public KafkaEventPublisher(
        IOptions<TOptions> options,
        ILogger<KafkaEventPublisher<TEvent, TOptions>> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers are not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Topic))
        {
            throw new InvalidOperationException("Kafka topic is not configured.");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public virtual async Task PublishAsync(TEvent message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var key = _options.KeySelector(message);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        var payload = JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<string, string>
        {
            Key = key,
            Value = payload
        };

        var result = await _producer.ProduceAsync(_options.Topic, kafkaMessage, cancellationToken);

        _logger.LogInformation(
            "Published {EventType} event to {TopicPartitionOffset} with key {Key}",
            typeof(TEvent).Name,
            result.TopicPartitionOffset,
            key);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

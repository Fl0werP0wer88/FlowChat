using System.Text.Json;
using Confluent.Kafka;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class KafkaUserCreatedEventPublisher : IUserCreatedEventPublisher, IDisposable
{
    private readonly UserCreatedProducerOptions _options;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaUserCreatedEventPublisher> _logger;

    public KafkaUserCreatedEventPublisher(
        IOptions<UserCreatedProducerOptions> options,
        ILogger<KafkaUserCreatedEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            throw new InvalidOperationException(
                $"Missing configuration value: {UserCreatedProducerOptions.SectionName}:BootstrapServers");
        }

        if (string.IsNullOrWhiteSpace(_options.Topic))
        {
            throw new InvalidOperationException(
                $"Missing configuration value: {UserCreatedProducerOptions.SectionName}:Topic");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(UserCreatedEvent message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = message.UserId.ToString(),
            Value = payload
        };

        var result = await _producer.ProduceAsync(_options.Topic, kafkaMessage, cancellationToken);

        _logger.LogInformation(
            "Published user-created event to {TopicPartitionOffset} for user {UserId}",
            result.TopicPartitionOffset,
            message.UserId);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

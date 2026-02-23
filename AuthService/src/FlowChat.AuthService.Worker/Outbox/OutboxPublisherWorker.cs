using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.Worker.Outbox;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly IOptions<OutboxPublisherOptions> _options;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory serviceScopeFactory,
        IProducer<string, string> producer,
        IOptions<OutboxPublisherOptions> options,
        ILogger<OutboxPublisherWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _producer = producer;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Value.PollIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing outbox messages.");
                await Task.Delay(TimeSpan.FromSeconds(_options.Value.PollIntervalSeconds), stoppingToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        return base.StopAsync(cancellationToken);
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var batchSize = Math.Max(1, _options.Value.BatchSize);

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .Where(message => message.NextRetryOnUtc == null || message.NextRetryOnUtc <= now)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        foreach (var message in messages)
        {
            await PublishMessageAsync(message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = message.Key ?? string.Empty,
                Value = message.Content,
                Headers = DeserializeHeaders(message.Headers)
            };

            var result = await _producer.ProduceAsync(message.Topic, kafkaMessage, cancellationToken);

            message.ProcessedOnUtc = DateTime.UtcNow;
            message.NextRetryOnUtc = null;
            message.Error = null;

            _logger.LogInformation(
                "Published outbox message {OutboxMessageId} of type {Type} to {TopicPartitionOffset}",
                message.Id,
                message.Type,
                result.TopicPartitionOffset);
        }
        catch (Exception ex)
        {
            message.RetryCount += 1;
            message.Error = ex.ToString();
            message.NextRetryOnUtc = DateTime.UtcNow.AddSeconds(CalculateRetryDelaySeconds(message.RetryCount));

            _logger.LogWarning(
                ex,
                "Failed to publish outbox message {OutboxMessageId}. Retry {RetryCount} scheduled for {NextRetryOnUtc}",
                message.Id,
                message.RetryCount,
                message.NextRetryOnUtc);
        }
    }

    private Headers? DeserializeHeaders(string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson))
        {
            return null;
        }

        try
        {
            var headersDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
            if (headersDictionary is null || headersDictionary.Count == 0)
            {
                return null;
            }

            var headers = new Headers();
            foreach (var header in headersDictionary)
            {
                headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
            }

            return headers;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Headers JSON for outbox message is invalid. Message will be published without headers.");
            return null;
        }
    }

    private int CalculateRetryDelaySeconds(int retryCount)
    {
        var baseDelay = Math.Max(1, _options.Value.RetryBaseDelaySeconds);
        var maxDelay = Math.Max(baseDelay, _options.Value.MaxRetryDelaySeconds);

        var exponentialDelay = baseDelay * Math.Pow(2, Math.Min(retryCount, 8));
        return (int)Math.Min(maxDelay, exponentialDelay);
    }
}

using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.Messaging.Runtime.Kafka.GenericProducer;

public sealed class GenericKafkaOutboxPublisherBackgroundService<TDbContext, TOutboxMessage> : BackgroundService
    where TDbContext : DbContext
    where TOutboxMessage : class, ILeaseableOutboxMessage
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly OutboxPublisherRuntimeOptions _options;
    private readonly HashSet<string> _allowedTopics;
    private readonly ILogger<GenericKafkaOutboxPublisherBackgroundService<TDbContext, TOutboxMessage>> _logger;

    public GenericKafkaOutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<OutboxPublisherRuntimeOptions> options,
        ILogger<GenericKafkaOutboxPublisherBackgroundService<TDbContext, TOutboxMessage>> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        _allowedTopics = _options.AllowedTopics
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (_allowedTopics.Count == 0)
        {
            throw new InvalidOperationException("No outbox topics configured in OutboxPublisher:AllowedTopics.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ValidateOptions(_options);
        _logger.LogInformation("Generic outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing outbox messages.");
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
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
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var now = DateTime.UtcNow;
        var leaseDurationSeconds = Math.Max(1, _options.LeaseDurationSeconds);
        var lockId = Guid.NewGuid();
        var lockExpiresAt = now.AddSeconds(leaseDurationSeconds);

        var messages = await ClaimBatchAsync(
            dbContext,
            now,
            lockId,
            lockExpiresAt,
            cancellationToken);

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

    private async Task PublishMessageAsync(TOutboxMessage message, CancellationToken cancellationToken)
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
            message.LockId = null;
            message.LockedUntilUtc = null;
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
            message.LockId = null;
            message.LockedUntilUtc = null;
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
        var baseDelay = Math.Max(1, _options.RetryBaseDelaySeconds);
        var maxDelay = Math.Max(baseDelay, _options.MaxRetryDelaySeconds);

        var exponentialDelay = baseDelay * Math.Pow(2, Math.Min(retryCount, 8));
        return (int)Math.Min(maxDelay, exponentialDelay);
    }

    private async Task<List<TOutboxMessage>> ClaimBatchAsync(
        TDbContext dbContext,
        DateTime now,
        Guid lockId,
        DateTime lockExpiresAt,
        CancellationToken cancellationToken)
    {
        var batchSize = Math.Max(1, _options.BatchSize);
        var entityType = dbContext.Model.FindEntityType(typeof(TOutboxMessage))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TOutboxMessage).Name}' is not mapped.");

        var storeObject = StoreObjectIdentifier.Table(
            entityType.GetTableName() ?? throw new InvalidOperationException($"Table name for '{typeof(TOutboxMessage).Name}' is missing."),
            entityType.GetSchema());

        var tableName = QuoteTable(entityType);
        var idColumn = GetColumnName(entityType, nameof(IOutboxMessage.Id), storeObject);
        var processedOnUtcColumn = GetColumnName(entityType, nameof(IOutboxMessage.ProcessedOnUtc), storeObject);
        var nextRetryOnUtcColumn = GetColumnName(entityType, nameof(IOutboxMessage.NextRetryOnUtc), storeObject);
        var occurredOnUtcColumn = GetColumnName(entityType, nameof(IOutboxMessage.OccurredOnUtc), storeObject);
        var topicColumn = GetColumnName(entityType, nameof(IOutboxMessage.Topic), storeObject);
        var lockIdColumn = GetColumnName(entityType, nameof(ILeaseableOutboxMessage.LockId), storeObject);
        var lockedUntilUtcColumn = GetColumnName(entityType, nameof(ILeaseableOutboxMessage.LockedUntilUtc), storeObject);
        var allowedTopicsSql = string.Join(", ", _allowedTopics.Select(QuoteStringLiteral));

        var sql = $"""
WITH claimed AS (
    SELECT {idColumn}
    FROM {tableName}
    WHERE {processedOnUtcColumn} IS NULL
      AND ({nextRetryOnUtcColumn} IS NULL OR {nextRetryOnUtcColumn} <= {ToTimestampLiteral(now)})
      AND ({lockedUntilUtcColumn} IS NULL OR {lockedUntilUtcColumn} < {ToTimestampLiteral(now)})
      AND {topicColumn} IN ({allowedTopicsSql})
    ORDER BY {occurredOnUtcColumn}
    FOR UPDATE SKIP LOCKED
    LIMIT {batchSize}
)
UPDATE {tableName} AS outbox
SET {lockIdColumn} = {ToUuidLiteral(lockId)},
    {lockedUntilUtcColumn} = {ToTimestampLiteral(lockExpiresAt)}
FROM claimed
WHERE outbox.{idColumn} = claimed.{idColumn}
RETURNING outbox.*;
""";

        return await dbContext.Set<TOutboxMessage>()
            .FromSqlRaw(sql)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }

    private static void ValidateOptions(OutboxPublisherRuntimeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers are not configured for the outbox publisher.");
        }

        if (options.BatchSize <= 0)
        {
            throw new InvalidOperationException("Outbox publisher batch size must be greater than zero.");
        }

        if (options.PollIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("Outbox publisher poll interval must be greater than zero.");
        }

        if (options.LeaseDurationSeconds <= 0)
        {
            throw new InvalidOperationException("Outbox publisher lease duration must be greater than zero.");
        }

        if (options.RetryBaseDelaySeconds <= 0)
        {
            throw new InvalidOperationException("Outbox publisher retry base delay must be greater than zero.");
        }

        if (options.MaxRetryDelaySeconds < options.RetryBaseDelaySeconds)
        {
            throw new InvalidOperationException("Outbox publisher max retry delay must be greater than or equal to base delay.");
        }
    }

    private static string QuoteTable(IEntityType entityType)
    {
        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"Table name for '{entityType.DisplayName()}' is missing.");

        var schema = entityType.GetSchema();
        return string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)}";
    }

    private static string GetColumnName(IEntityType entityType, string propertyName, StoreObjectIdentifier storeObject)
    {
        var property = entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' is not mapped on '{entityType.DisplayName()}'.");

        var columnName = property.GetColumnName(storeObject)
            ?? throw new InvalidOperationException($"Column for property '{propertyName}' on '{entityType.DisplayName()}' is missing.");

        return QuoteIdentifier(columnName);
    }

    private static string QuoteIdentifier(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string QuoteStringLiteral(string value) => $"'{value.Replace("'", "''")}'";

    private static string ToTimestampLiteral(DateTime value) =>
        $"TIMESTAMPTZ {QuoteStringLiteral(value.ToUniversalTime().ToString("O"))}";

    private static string ToUuidLiteral(Guid value) => $"'{value:D}'";
}

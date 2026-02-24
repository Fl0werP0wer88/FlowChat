using System.Text.Json;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.Persistence.Outbox;

public sealed class OutboxRepository<TEvent, TOptions> : IOutboxRepository<TEvent>
    where TOptions : class, IOutboxRepositoryOptions<TEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly TOptions _options;
    private readonly ILogger<OutboxRepository<TEvent, TOptions>> _logger;

    public OutboxRepository(
        AppDbContext dbContext,
        IOptions<TOptions> options,
        ILogger<OutboxRepository<TEvent, TOptions>> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task EnqueueAsync(TEvent message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(_options.Topic))
        {
            throw new InvalidOperationException("Outbox topic is not configured.");
        }

        var key = _options.KeySelector(message);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Outbox message key cannot be null or empty.");
        }

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TEvent).FullName ?? typeof(TEvent).Name,
            Topic = _options.Topic,
            Key = key,
            Content = JsonSerializer.Serialize(message),
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        _dbContext.OutboxMessages.Add(outboxMessage);

        _logger.LogInformation(
            "Outbox message {OutboxMessageId} of type {Type} queued for topic {Topic}.",
            outboxMessage.Id,
            outboxMessage.Type,
            outboxMessage.Topic);

        return Task.CompletedTask;
    }
}

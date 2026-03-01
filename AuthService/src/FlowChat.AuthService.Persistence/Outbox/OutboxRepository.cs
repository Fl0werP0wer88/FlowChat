using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.Persistence.Outbox;

public sealed class OutboxRepository<TEvent, TOptions> : IOutboxRepository<TEvent>
    where TEvent : class
    where TOptions : class, IOutboxRepositoryOptions<TEvent>
{
    private readonly IPublisher _publisher;
    private readonly TOptions _options;
    private readonly ILogger<OutboxRepository<TEvent, TOptions>> _logger;

    public OutboxRepository(
        IPublisher publisher,
        IOptions<TOptions> options,
        ILogger<OutboxRepository<TEvent, TOptions>> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnqueueAsync(TEvent message, CancellationToken cancellationToken)
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

        await _publisher.WrapAndPublishAsync(
            message,
            envelope => envelope.SetKafkaKey(key),
            cancellationToken);

        _logger.LogInformation(
            "Silverback outbox message of type {Type} queued for topic {Topic}.",
            typeof(TEvent).FullName ?? typeof(TEvent).Name,
            _options.Topic);
    }
}

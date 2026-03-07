using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using AutoMapper;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class SilverbackEventPublisher : IIntegrationEventPublisher
{
    private readonly IMapper _mapper;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublisher _publisher;
    private readonly ILogger<SilverbackEventPublisher> _logger;

    public SilverbackEventPublisher(
        IMapper mapper,
        IServiceProvider serviceProvider,
        IPublisher publisher,
        ILogger<SilverbackEventPublisher> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : DomainEventBase
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        return message switch
        {
            UserCreatedDomainEvent userCreated => PublishAsync(
                _mapper.Map<IntegrationEventEnvelope<UserCreatedIntegrationEvent>>(userCreated),
                cancellationToken),

            AccountConfirmedDomainEvent accountConfirmed => PublishAsync(
                _mapper.Map<IntegrationEventEnvelope<UserConfirmedIntegrationEvent>>(accountConfirmed),
                cancellationToken),

            EmailVerificationRequestedDomainEvent emailVerificationRequested => PublishAsync(
                _mapper.Map<IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>>(emailVerificationRequested),
                cancellationToken),

            _ => throw new NotSupportedException(
                $"Integration event mapping for domain event '{message.GetType().FullName}' is not configured.")
        };
    }

    private async Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var options = _serviceProvider.GetService<IKafkaProducerOptions<TEvent>>();
        if (options is null)
        {
            throw new InvalidOperationException($"Kafka producer options for event '{typeof(TEvent).FullName}' are not registered.");
        }

        var key = message.KafkaKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        await _publisher.WrapAndPublishAsync(
            message.Payload,
            envelope => EnrichEnvelope(envelope, message),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to Silverback producer for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            options.Topic,
            key);
    }

    private static void EnrichEnvelope<TEvent>(
        IOutboundEnvelope envelope,
        IntegrationEventEnvelope<TEvent> message)
        where TEvent : IntegrationEvent
    {
        envelope.SetKafkaKey(message.KafkaKey);
        foreach (var header in message.Headers)
        {
            envelope.AddHeader(header.Key, header.Value);
        }
    }
}

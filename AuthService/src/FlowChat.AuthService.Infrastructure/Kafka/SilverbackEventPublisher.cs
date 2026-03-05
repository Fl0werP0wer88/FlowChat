using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublisher _publisher;
    private readonly ILogger<SilverbackEventPublisher> _logger;

    public SilverbackEventPublisher(
        IServiceProvider serviceProvider,
        IPublisher publisher,
        ILogger<SilverbackEventPublisher> logger)
    {
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
                new IntegrationEventEnvelope<UserCreatedIntegrationEvent>(
                    new UserCreatedIntegrationEvent
                    {
                        UserId = userCreated.UserId,
                        Email = userCreated.Email,
                        PhoneNumber = userCreated.PhoneNumber,
                        UserName = userCreated.UserName,
                        DisplayName = userCreated.UserName,
                        FirstName = userCreated.FirstName,
                        LastName = userCreated.LastName
                    },
                    userCreated.UserId.ToString()),
                cancellationToken),

            AccountConfirmedDomainEvent accountConfirmed => PublishAsync(
                new IntegrationEventEnvelope<UserConfirmedIntegrationEvent>(
                    new UserConfirmedIntegrationEvent
                    {
                        UserId = accountConfirmed.UserId
                    },
                    accountConfirmed.UserId.ToString()),
                cancellationToken),

            EmailVerificationRequestedDomainEvent emailVerificationRequested => PublishAsync(
                new IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>(
                    new EmailVerificationRequestIntegrationEvent
                    {
                        UserId = emailVerificationRequested.UserId,
                        UserEmail = emailVerificationRequested.UserEmail,
                        ConfirmationLink = emailVerificationRequested.ConfirmationLink
                    },
                    emailVerificationRequested.UserId.ToString()),
                cancellationToken),

            _ => throw new NotSupportedException(
                $"Integration event mapping for domain event '{message.GetType().FullName}' is not configured.")
        };
    }

    private async Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
        where TEvent : IntegrationEventBase
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
        where TEvent : IntegrationEventBase
    {
        envelope.SetKafkaKey(message.KafkaKey);
        foreach (var header in message.Headers)
        {
            envelope.AddHeader(header.Key, header.Value);
        }
    }
}

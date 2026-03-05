using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class MassTransitEventPublisher : IIntegrationEventPublisher
{
    private readonly ITopicProducer<string, AuthIdentityEventEnvelopeV1> _identityEventsProducer;
    private readonly IKafkaProducerOptions<UserCreatedIntegrationEvent> _userCreatedOptions;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        ITopicProducer<string, AuthIdentityEventEnvelopeV1> identityEventsProducer,
        IKafkaProducerOptions<UserCreatedIntegrationEvent> userCreatedOptions,
        ILogger<MassTransitEventPublisher> logger)
    {
        _identityEventsProducer = identityEventsProducer ?? throw new ArgumentNullException(nameof(identityEventsProducer));
        _userCreatedOptions = userCreatedOptions ?? throw new ArgumentNullException(nameof(userCreatedOptions));
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

    private async Task PublishAsync<TEvent>(
        IntegrationEventEnvelope<TEvent> message,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEventBase
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.KafkaKey))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        var envelopeMessage = new AuthIdentityEventEnvelopeV1
        {
            EventType = typeof(TEvent).Name,
            EventVersion = 1,
            Payload = JsonSerializer.SerializeToElement(message.Payload)
        };

        await _identityEventsProducer.Produce(
            message.KafkaKey,
            envelopeMessage,
            Pipe.Execute<KafkaSendContext<string, AuthIdentityEventEnvelopeV1>>(context =>
            {
                foreach (var header in message.Headers)
                {
                    context.Headers.Set(header.Key, header.Value);
                }

                context.Headers.Set(IntegrationMessageHeaders.EventType, envelopeMessage.EventType);
            }),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to MassTransit outbox for topic {Topic} with key {Key}.",
            envelopeMessage.EventType,
            _userCreatedOptions.Topic,
            message.KafkaKey);
    }
}

using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class MassTransitEventPublisher : IIntegrationEventPublisher
{
    private readonly ITopicProducer<string, AuthIdentityTopicEventBase> _identityEventsProducer;
    private readonly IKafkaProducerOptions<UserCreatedIntegrationEvent> _userCreatedOptions;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        ITopicProducer<string, AuthIdentityTopicEventBase> identityEventsProducer,
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
        where TEvent : AuthIdentityTopicEventBase
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.KafkaKey))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        await _identityEventsProducer.Produce(
            message.KafkaKey,
            message.Payload,
            Pipe.Execute<KafkaSendContext<string, AuthIdentityTopicEventBase>>(context =>
            {
                foreach (var header in message.Headers)
                {
                    context.Headers.Set(header.Key, header.Value);
                }

                context.Headers.Set(IntegrationMessageHeaders.EventType, typeof(TEvent).Name);
            }),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to MassTransit outbox for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            _userCreatedOptions.Topic,
            message.KafkaKey);
    }
}

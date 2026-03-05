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
    private readonly ITopicProducer<string, UserCreatedIntegrationEvent> _userCreatedProducer;
    private readonly ITopicProducer<string, UserConfirmedIntegrationEvent> _userConfirmedProducer;
    private readonly ITopicProducer<string, EmailVerificationRequestIntegrationEvent> _emailVerificationRequestedProducer;
    private readonly IKafkaProducerOptions<UserCreatedIntegrationEvent> _userCreatedOptions;
    private readonly IKafkaProducerOptions<UserConfirmedIntegrationEvent> _userConfirmedOptions;
    private readonly IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent> _emailVerificationRequestedOptions;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        ITopicProducer<string, UserCreatedIntegrationEvent> userCreatedProducer,
        ITopicProducer<string, UserConfirmedIntegrationEvent> userConfirmedProducer,
        ITopicProducer<string, EmailVerificationRequestIntegrationEvent> emailVerificationRequestedProducer,
        IKafkaProducerOptions<UserCreatedIntegrationEvent> userCreatedOptions,
        IKafkaProducerOptions<UserConfirmedIntegrationEvent> userConfirmedOptions,
        IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent> emailVerificationRequestedOptions,
        ILogger<MassTransitEventPublisher> logger)
    {
        _userCreatedProducer = userCreatedProducer ?? throw new ArgumentNullException(nameof(userCreatedProducer));
        _userConfirmedProducer = userConfirmedProducer ?? throw new ArgumentNullException(nameof(userConfirmedProducer));
        _emailVerificationRequestedProducer = emailVerificationRequestedProducer ?? throw new ArgumentNullException(nameof(emailVerificationRequestedProducer));
        _userCreatedOptions = userCreatedOptions ?? throw new ArgumentNullException(nameof(userCreatedOptions));
        _userConfirmedOptions = userConfirmedOptions ?? throw new ArgumentNullException(nameof(userConfirmedOptions));
        _emailVerificationRequestedOptions = emailVerificationRequestedOptions ?? throw new ArgumentNullException(nameof(emailVerificationRequestedOptions));
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
                _userCreatedProducer,
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
                _userCreatedOptions.Topic,
                cancellationToken),

            AccountConfirmedDomainEvent accountConfirmed => PublishAsync(
                _userConfirmedProducer,
                new IntegrationEventEnvelope<UserConfirmedIntegrationEvent>(
                    new UserConfirmedIntegrationEvent
                    {
                        UserId = accountConfirmed.UserId
                    },
                    accountConfirmed.UserId.ToString()),
                _userConfirmedOptions.Topic,
                cancellationToken),

            EmailVerificationRequestedDomainEvent emailVerificationRequested => PublishAsync(
                _emailVerificationRequestedProducer,
                new IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>(
                    new EmailVerificationRequestIntegrationEvent
                    {
                        UserId = emailVerificationRequested.UserId,
                        UserEmail = emailVerificationRequested.UserEmail,
                        ConfirmationLink = emailVerificationRequested.ConfirmationLink
                    },
                    emailVerificationRequested.UserId.ToString()),
                _emailVerificationRequestedOptions.Topic,
                cancellationToken),

            _ => throw new NotSupportedException(
                $"Integration event mapping for domain event '{message.GetType().FullName}' is not configured.")
        };
    }

    private async Task PublishAsync<TEvent>(
        ITopicProducer<string, TEvent> producer,
        IntegrationEventEnvelope<TEvent> message,
        string topic,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.KafkaKey))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        await producer.Produce(
            message.KafkaKey,
            message.Payload,
            Pipe.Execute<KafkaSendContext<string, TEvent>>(context =>
            {
                foreach (var header in message.Headers)
                {
                    context.Headers.Set(header.Key, header.Value);
                }
            }),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to MassTransit outbox for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            topic,
            message.KafkaKey);
    }
}

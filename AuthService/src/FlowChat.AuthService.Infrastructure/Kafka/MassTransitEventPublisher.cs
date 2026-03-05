using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class MassTransitEventPublisher : IIntegrationEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitEventPublisher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
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
        where TEvent : IntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        var options = _serviceProvider.GetService<IKafkaProducerOptions<TEvent>>();

        await _publishEndpoint.Publish(
            message.Payload,
            context =>
            {
                foreach (var header in message.Headers)
                {
                    context.Headers.Set(header.Key, header.Value);
                }
            },
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to MassTransit outbox for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            options?.Topic ?? "n/a",
            message.KafkaKey);
    }
}

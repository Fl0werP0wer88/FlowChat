using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.AuthService.Persistence;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class WolverineEventPublisher : IIntegrationEventPublisher
{
    private readonly IDbContextOutbox<AppDbContext> _outbox;
    private readonly ILogger<WolverineEventPublisher> _logger;

    public WolverineEventPublisher(
        IDbContextOutbox<AppDbContext> outbox,
        ILogger<WolverineEventPublisher> logger)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
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
        where TEvent : IntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        var deliveryOptions = new DeliveryOptions
        {
            PartitionKey = message.KafkaKey
        };

        foreach (var header in message.Headers)
        {
            deliveryOptions.WithHeader(header.Key, header.Value);
        }

        await _outbox.PublishAsync(message.Payload, deliveryOptions);

        _logger.LogInformation(
            "Captured {EventType} event in Wolverine outbox with key {Key}.",
            typeof(TEvent).Name,
            message.KafkaKey);
    }
}

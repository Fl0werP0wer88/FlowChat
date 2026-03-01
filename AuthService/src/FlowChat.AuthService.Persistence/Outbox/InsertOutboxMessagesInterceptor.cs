using System.Text.Json;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Messaging.Contracts.AuthService.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.Persistence.Outbox;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private readonly UserCreatedProducerOptions _userCreatedProducerOptions;
    private readonly IPublisher _publisher;

    public InsertOutboxMessagesInterceptor(
        IOptions<UserCreatedProducerOptions> userCreatedProducerOptions,
        IPublisher publisher)
    {
        _userCreatedProducerOptions = userCreatedProducerOptions?.Value
            ?? throw new ArgumentNullException(nameof(userCreatedProducerOptions));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddOutboxMessagesAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsyncInternal(eventData, result, cancellationToken);
    }

    private async ValueTask<InterceptionResult<int>> SaveChangesAsyncInternal(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken)
    {
        await AddOutboxMessagesAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task AddOutboxMessagesAsync(DbContext? dbContext, CancellationToken cancellationToken)
    {
        if (dbContext is null)
        {
            return;
        }

        var aggregatesWithDomainEvents = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        if (aggregatesWithDomainEvents.Count == 0)
        {
            return;
        }

        foreach (var aggregate in aggregatesWithDomainEvents)
        {
            foreach (var outboxEvent in aggregate.DomainEvents.OfType<IOutboxDomainEvent>())
            {
                await PublishIntegrationEventAsync(outboxEvent, cancellationToken);
            }

            aggregate.ClearDomainEvents();
        }
    }

    private Task PublishIntegrationEventAsync(IOutboxDomainEvent outboxEvent, CancellationToken cancellationToken) =>
        outboxEvent switch
        {
            UserCreatedDomainEvent userCreatedDomainEvent => PublishAsync(
                new UserCreatedIntegrationEvent
                {
                    UserId = userCreatedDomainEvent.UserId,
                    UserName = userCreatedDomainEvent.UserName,
                    DisplayName = userCreatedDomainEvent.DisplayName,
                    Email = userCreatedDomainEvent.Email
                },
                userCreatedDomainEvent.UserId.ToString(),
                cancellationToken),
            AccountConfirmedDomainEvent accountConfirmedDomainEvent => PublishAsync(
                new UserConfirmedIntegrationEvent
                {
                    UserId = accountConfirmedDomainEvent.UserId
                },
                accountConfirmedDomainEvent.UserId.ToString(),
                cancellationToken),
            _ => throw new InvalidOperationException($"No outbox mapping found for outbox event type '{outboxEvent.GetType().Name}'.")
        };

    private async Task PublishAsync<TMessage>(TMessage message, string key, CancellationToken cancellationToken)
        where TMessage : class
    {
        if (string.IsNullOrWhiteSpace(_userCreatedProducerOptions.Topic))
        {
            throw new InvalidOperationException("Kafka topic is not configured for AuthService integration events.");
        }

        await _publisher.WrapAndPublishAsync(
            message,
            envelope => envelope.SetKafkaKey(key),
            cancellationToken);
    }
}

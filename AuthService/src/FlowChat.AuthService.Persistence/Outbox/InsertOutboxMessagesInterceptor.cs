using System.Text.Json;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Messaging.Contracts.AuthService.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.Persistence.Outbox;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly UserCreatedProducerOptions _userCreatedProducerOptions;

    public InsertOutboxMessagesInterceptor(IOptions<UserCreatedProducerOptions> userCreatedProducerOptions)
    {
        _userCreatedProducerOptions = userCreatedProducerOptions?.Value
            ?? throw new ArgumentNullException(nameof(userCreatedProducerOptions));
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddOutboxMessages(DbContext? dbContext)
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

        var outboxMessages = new List<OutboxMessage>();

        foreach (var aggregate in aggregatesWithDomainEvents)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                outboxMessages.Add(MapToOutboxMessage(domainEvent));
            }

            aggregate.ClearDomainEvents();
        }

        dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
    }

    private OutboxMessage MapToOutboxMessage(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            UserCreatedDomainEvent userCreatedDomainEvent => CreateUserCreatedMessage(userCreatedDomainEvent),
            _ => throw new InvalidOperationException($"No outbox mapping found for domain event type '{domainEvent.GetType().Name}'.")
        };
    }

    private OutboxMessage CreateUserCreatedMessage(UserCreatedDomainEvent domainEvent)
    {
        var integrationEvent = new UserCreatedEvent
        {
            UserId = domainEvent.UserId,
            UserName = domainEvent.UserName,
            DisplayName = domainEvent.DisplayName,
            Email = domainEvent.Email
        };

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(UserCreatedEvent).FullName ?? nameof(UserCreatedEvent),
            Topic = _userCreatedProducerOptions.Topic,
            Key = domainEvent.UserId.ToString(),
            Content = JsonSerializer.Serialize(integrationEvent, JsonSerializerOptions),
            OccurredOnUtc = domainEvent.OccurredOnUtc,
            RetryCount = 0
        };
    }
}

using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using DomainValidationException = FlowChat.Shared.Domain.Exceptions.ValidationException;

namespace FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;

public abstract class AggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    : Notifications.DomainEventHandlerBase<TNotification>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IEnumerable<IAggregateBeforeSaveProcessor<TNotification, TAggregate>> _beforeSaveProcessors;
    private MutationType _mutationType = MutationType.Unchanged;

    protected AggregateRootDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<TNotification, TAggregate>> beforeSaveProcessors)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors;
    }

    protected TAggregate? AggregateRoot { get; set; }

    protected override async Task HandleNotificationAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var fetchResult = await FetchAggregateRootAsync(notification, cancellationToken);
        if (fetchResult.IsFailure)
        {
            throw new ResultException(FlowChatResult.Failure(fetchResult.Error));
        }

        if (fetchResult.Value is not null)
        {
            AggregateRoot = fetchResult.Value;
            CapturePreMutationSnapshot(AggregateRoot);
        }

        var operationResult = await ExecuteAsync(notification, cancellationToken);

        if (operationResult.IsFailure)
        {
            throw new ResultException(operationResult);
        }

        var aggregateRoot = GetAggregateRoot();

        if (_mutationType == MutationType.Unchanged)
        {
            return;
        }

        aggregateRoot.IncrementVersion();
        var domainEvents = aggregateRoot.PopDomainEvents();
        var localEvents = domainEvents
            .Cast<ILocalEvent>();

        await DispatchLocalEventsAsync(localEvents, cancellationToken);
        ApplyAuditInfo(aggregateRoot, _mutationType);

        foreach (var processor in _beforeSaveProcessors)
        {
            await processor.ProcessAsync(notification, aggregateRoot, _mutationType, cancellationToken);
        }
    }

    protected void SetMutationType(MutationType mutationType)
    {
        _mutationType = mutationType;
    }

    protected void CapturePreMutationSnapshot(TAggregate aggregate)
    {
        foreach (var processor in _beforeSaveProcessors)
        {
            processor.CaptureBeforeState(aggregate);
        }
    }

    protected virtual Task<FlowChatResult<TAggregate?>> FetchAggregateRootAsync(
        TNotification notification,
        CancellationToken cancellationToken)
        => Task.FromResult(FlowChatResult<TAggregate?>.Success(default));

    protected abstract Task<FlowChatResult> ExecuteAsync(TNotification notification, CancellationToken cancellationToken);

    protected virtual TAggregate GetAggregateRoot() =>
        AggregateRoot ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    private static void ApplyAuditInfo(TAggregate aggregateRoot, MutationType mutationType)
    {
        const string SystemActor = "system";

        switch (mutationType)
        {
            case MutationType.Created:
                aggregateRoot.SetCreated(SystemActor);
                aggregateRoot.SetUpdated(SystemActor);
                break;
            case MutationType.Updated:
                aggregateRoot.SetUpdated(SystemActor);
                break;
            case MutationType.Deleted:
                aggregateRoot.SetUpdated(SystemActor);
                aggregateRoot.Delete(UtcDateTimeOffset.UtcNow);
                break;
            case MutationType.Unchanged:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutationType), mutationType, null);
        }
    }

    protected Task DispatchLocalEventsAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _localEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}

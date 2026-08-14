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
    private readonly IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> _beforeSaveProcessors;

    protected AggregateRootDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> beforeSaveProcessors)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors;
    }

    protected TAggregate? AggregateRoot { get; set; }

    protected override async Task HandleNotificationAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var executionResult = await ExecuteAsync(notification, cancellationToken);

        if (executionResult.IsFailure)
        {
            throw new ResultException(FlowChatResult.Failure(executionResult.Error));
        }

        var mutationType = executionResult.Value;
        var aggregateRoot = GetAggregateRoot();

        if (mutationType == MutationType.Unchanged)
        {
            return;
        }

        aggregateRoot.IncrementVersion();
        var domainEvents = aggregateRoot.PopDomainEvents();
        DomainEventBase.StampVersions(domainEvents, aggregateRoot.Version);
        var localEvents = domainEvents
            .Cast<ILocalEvent>();

        await DispatchLocalEventsAsync(localEvents, cancellationToken);
        ApplyAuditInfo(aggregateRoot, mutationType);

        foreach (var processor in _beforeSaveProcessors)
        {
            await processor.ProcessAsync(notification, aggregateRoot, mutationType, cancellationToken);
        }
    }

    protected static FlowChatResult<MutationType> Unchanged() =>
        Mutation(MutationType.Unchanged);

    protected static FlowChatResult<MutationType> Failure(IDomainError error) =>
        FlowChatResult<MutationType>.Failure(error);

    protected static FlowChatResult<MutationType> Mutation(MutationType mutationType) =>
        FlowChatResult<MutationType>.Success(mutationType);

    protected abstract Task<FlowChatResult<MutationType>> ExecuteAsync(
        TNotification notification,
        CancellationToken cancellationToken);

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

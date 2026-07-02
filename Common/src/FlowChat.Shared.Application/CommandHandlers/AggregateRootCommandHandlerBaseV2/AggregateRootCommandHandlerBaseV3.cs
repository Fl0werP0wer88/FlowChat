using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class AggregateRootCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    : TransactionalCommandHandlerBase<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> _beforeSaveProcessors;
    private MutationType _mutationType = MutationType.Unchanged;

    protected AggregateRootCommandHandlerBaseV3(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> beforeSaveProcessors)
        : base(unitOfWork)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors;
    }

    protected override async Task<FlowChatResult<TResponse>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var operationResult = await ExecuteAsync(request, cancellationToken);

        if (operationResult.IsSuccess)
        {
            var aggregateRoot = GetAggregateRoot();

            if (_mutationType == MutationType.Unchanged)
            {
                return operationResult;
            }

            aggregateRoot.IncrementVersion();
            var domainEvents = aggregateRoot.PopDomainEvents();
            var localEvents = domainEvents
                .Cast<ILocalEvent>();

            await DispatchLocalEventsAsync(localEvents, cancellationToken);
            ApplyAuditInfo(aggregateRoot, _mutationType);

            foreach (var processor in _beforeSaveProcessors)
            {
                await processor.ProcessAsync(request, aggregateRoot, _mutationType, cancellationToken);
            }
        }

        return operationResult;
    }

    protected void SetMutationType(MutationType mutationType)
    {
        _mutationType = mutationType;
    }

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected abstract TAggregate GetAggregateRoot();

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

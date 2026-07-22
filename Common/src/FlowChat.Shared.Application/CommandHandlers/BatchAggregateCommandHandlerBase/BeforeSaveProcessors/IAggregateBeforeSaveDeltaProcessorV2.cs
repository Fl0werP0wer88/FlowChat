using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public interface IAggregateBeforeSaveDeltaProcessorV2<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot
{
    Task ProcessAsync(
        TCommand command,
        AggregateDeltaBatch<TAggregate> batch,
        CancellationToken cancellationToken);
}

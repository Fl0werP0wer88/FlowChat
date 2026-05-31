using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public interface IAggregateBeforeSaveProcessor<TCommand, TAggregate>
{
    Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        OperationType operationType,
        CancellationToken cancellationToken);
}

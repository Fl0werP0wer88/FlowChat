using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public interface IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>
{
    Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken);
}

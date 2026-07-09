using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public interface IAggregateBeforeSaveProcessor<TCommand, TAggregate>
{
    Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken);

    void CaptureBeforeState(TAggregate aggregate)
    {
    }
}

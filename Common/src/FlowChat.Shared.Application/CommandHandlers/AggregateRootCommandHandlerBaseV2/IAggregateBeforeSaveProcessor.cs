namespace FlowChat.Shared.Application;

public interface IAggregateBeforeSaveProcessor<TCommand, TAggregate>
{
    Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        CancellationToken cancellationToken);
}

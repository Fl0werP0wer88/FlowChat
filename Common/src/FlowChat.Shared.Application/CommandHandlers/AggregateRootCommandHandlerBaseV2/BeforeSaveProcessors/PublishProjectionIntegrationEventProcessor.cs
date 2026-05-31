namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public class PublishProjectionIntegrationEvent<TCommand, TAggregate> : IAggregateBeforeSaveProcessor<TCommand, TAggregate>
{
    public Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public class PublishProjectionIntegrationEventProcessor<TCommand, TAggregate, TTargetReadModel> : IAggregateBeforeSaveProcessor<TCommand, TAggregate>
{
    public Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public interface IAggregateDeltaProjectionKeyProviderV2<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot
{
    string GetKafkaKey(
        TCommand command,
        IReadOnlyList<AggregateDeltaMutation<TAggregate>> mutations);
}

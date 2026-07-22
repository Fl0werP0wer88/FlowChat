using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public interface IAggregateDeltaProjectionMetadataProviderV2<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot
{
    Guid GetProjectionId(
        TCommand command,
        IReadOnlyList<AggregateDeltaMutation<TAggregate>> mutations);

    int GetProjectionRevision(
        TCommand command,
        IReadOnlyList<AggregateDeltaMutation<TAggregate>> mutations);

    string GetKafkaKey(
        TCommand command,
        IReadOnlyList<AggregateDeltaMutation<TAggregate>> mutations);
}

using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors.Interfaces;

public interface IDeltaProjectionRevisionProvider<in TAggregate, TValue>
    where TAggregate : IAggregateRoot
    where TValue : notnull
{
    int GetRevision(TAggregate aggregate);
}

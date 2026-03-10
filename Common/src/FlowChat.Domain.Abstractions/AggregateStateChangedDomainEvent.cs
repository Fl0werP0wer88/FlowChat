namespace FlowChat.Domain.Abstractions;

public sealed class AggregateStateChangedDomainEvent<TRoot, TSnapshot> : DomainEventBase, IAggregateStateChangedDomainEvent
    where TRoot : AggregateRootBase<TRoot>
{
    public AggregateStateChangedDomainEvent(
        Id<TRoot> aggregateId,
        string aggregateType,
        TSnapshot aggregateState)
        : base(aggregateId, aggregateType, DateTimeOffset.UtcNow)
    {
        AggregateState = aggregateState;
    }

    public TSnapshot AggregateState { get; }
}

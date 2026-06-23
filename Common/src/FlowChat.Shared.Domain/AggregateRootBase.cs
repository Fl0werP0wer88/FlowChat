namespace FlowChat.Shared.Domain;

public abstract class AggregateRootBase<TDomainEntity> : EntityBase<TDomainEntity>, IAggregateRoot
    where TDomainEntity : AggregateRootBase<TDomainEntity>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRootBase(Id<TDomainEntity> id) : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        ClearEvents();
        return events;
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    protected void MarkAggregateStateChanged<TSnapshot>(string aggregateType, Func<TSnapshot> snapshotFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentNullException.ThrowIfNull(snapshotFactory);

        // Replace any existing snapshot event for this aggregate so only the latest state
        // is published per transaction — multiple mutations in one operation emit one snapshot.
        var existingEvent = _domainEvents.FirstOrDefault(
            domainEvent => domainEvent is IAggregateStateChangedDomainEvent && domainEvent.AggregateId == Id.Value);

        if (existingEvent is not null)
        {
            _domainEvents.Remove(existingEvent);
        }

        _domainEvents.Add(new AggregateStateChangedDomainEvent<TDomainEntity, TSnapshot>(
            Id,
            aggregateType,
            snapshotFactory()));
    }

    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Remove(domainEvent);
    }

    public void ClearEvents()
    {
        _domainEvents.Clear();
    }
}


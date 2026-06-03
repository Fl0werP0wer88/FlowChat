namespace FlowChat.Shared.Domain;

public abstract class AggregateRootBase<TDomainEntity> : EntityBase<TDomainEntity>, IAggregateRoot
    where TDomainEntity : AggregateRootBase<TDomainEntity>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public int Version { get; private set; } = 1;

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

    public void IncrementVersion()
    {
        Version++;
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
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


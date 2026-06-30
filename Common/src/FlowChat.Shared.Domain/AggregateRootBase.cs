using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public abstract class AggregateRootBase<TDomainEntity> : EntityBase<TDomainEntity>, IAggregateRoot
    where TDomainEntity : AggregateRootBase<TDomainEntity>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string CreatedBy { get; private set; } = string.Empty;
    public UtcDateTimeOffset CreatedAtUtc { get; private set; } = null!;
    public string LastModifiedBy { get; private set; } = string.Empty;
    public UtcDateTimeOffset LastModifiedAtUtc { get; private set; } = null!;
    public int Version { get; private set; } = 1;
    public UtcDateTimeOffset? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt is not null;

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

    public void SetCreated(string createdBy)
    {
        ArgumentNullException.ThrowIfNull(createdBy);

        CreatedBy = createdBy;
        CreatedAtUtc = UtcDateTimeOffset.UtcNow;
    }

    public void SetUpdated(string lastModifiedBy)
    {
        ArgumentNullException.ThrowIfNull(lastModifiedBy);

        LastModifiedBy = lastModifiedBy;
        LastModifiedAtUtc = UtcDateTimeOffset.UtcNow;
    }

    public void Delete(UtcDateTimeOffset deletedAt)
    {
        ArgumentNullException.ThrowIfNull(deletedAt);

        if (DeletedAt is not null)
        {
            return;
        }

        DeletedAt = deletedAt;
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


using System.ComponentModel.DataAnnotations.Schema;

namespace FlowChat.SocialGraphService.Domain.Common;

public abstract class AggregateRootBase : EntityBase, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected  AggregateRootBase(Guid id) :base(id){}

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

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

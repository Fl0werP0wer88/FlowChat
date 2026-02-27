using System.ComponentModel.DataAnnotations.Schema;
using FlowChat.SocialGraphService.Domain.Events.Contracts;

namespace FlowChat.SocialGraphService.Domain.Common.Contracts;

public abstract class AggregateRootBase : EntityBase, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRootBase(Guid id) : base(id) { }

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

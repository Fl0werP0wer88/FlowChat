using System.ComponentModel.DataAnnotations.Schema;
using FlowChat.AuthService.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace FlowChat.AuthService.Persistence.Identity;

public class AppUser : IdentityUser<Guid>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void AddDomainEvents(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            AddDomainEvent(domainEvent);
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

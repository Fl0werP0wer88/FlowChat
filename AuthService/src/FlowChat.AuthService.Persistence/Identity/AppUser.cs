using System.ComponentModel.DataAnnotations.Schema;
using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Events;
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

    public void AddUserCreatedDomainEvent()
    {
        var userName = UserName ?? string.Empty;
        AddDomainEvent(new UserCreatedDomainEvent(Id, userName, userName, Email ?? string.Empty));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

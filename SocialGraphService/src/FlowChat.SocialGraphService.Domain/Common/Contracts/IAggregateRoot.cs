using FlowChat.SocialGraphService.Domain.Events.Contracts;

namespace FlowChat.SocialGraphService.Domain.Common.Contracts;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearEvents();
    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}

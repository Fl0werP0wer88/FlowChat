namespace FlowChat.Domain.Abstractions;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearEvents();

    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}

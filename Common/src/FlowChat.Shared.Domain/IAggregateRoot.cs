namespace FlowChat.Shared.Domain;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearEvents();

    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}


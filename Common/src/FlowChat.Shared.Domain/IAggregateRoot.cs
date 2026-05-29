namespace FlowChat.Shared.Domain;

public interface IAggregateRoot : IVersionedEntity
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearEvents();

    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}


namespace FlowChat.Shared.Domain;

public interface IAggregateRoot : IVersionedEntity, ISoftDeletable
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearEvents();

    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}


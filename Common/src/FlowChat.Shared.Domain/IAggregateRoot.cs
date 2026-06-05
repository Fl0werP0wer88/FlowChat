namespace FlowChat.Shared.Domain;

public interface IAggregateRoot : IVersionedEntity, ISoftDeletable, IAuditableEntity
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearEvents();

    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}


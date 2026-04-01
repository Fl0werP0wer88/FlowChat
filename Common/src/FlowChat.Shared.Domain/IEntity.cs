namespace FlowChat.Shared.Domain;

public interface IEntity<TDomainEntity> : IAuditableEntity, IVersionedEntity
{
    Id<TDomainEntity> Id { get; }
}


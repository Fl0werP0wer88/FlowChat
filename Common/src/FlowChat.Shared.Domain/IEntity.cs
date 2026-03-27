namespace FlowChat.Shared.Domain;

public interface IEntity<TDomainEntity> : IAuditableEntity
{
    Id<TDomainEntity> Id { get; }
}


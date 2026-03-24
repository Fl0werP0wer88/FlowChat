namespace FlowChat.Domain.Abstractions;

public interface IEntity<TDomainEntity> : IAuditableEntity
{
    Id<TDomainEntity> Id { get; }
}

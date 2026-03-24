namespace FlowChat.Domain.Abstractions;

public abstract class EntityBase<TDomainEntity>
    : AuditableEntityBase, IEntity<TDomainEntity>
    where TDomainEntity : EntityBase<TDomainEntity>
{
    public Id<TDomainEntity> Id { get; }

    protected EntityBase() : this(Id<TDomainEntity>.New()) { }
    protected EntityBase(Id<TDomainEntity>? id)
    {
        Id = id ?? Id<TDomainEntity>.New();
    }
}

using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public abstract class EntityBase<TDomainEntity>
    : IEntity<TDomainEntity>
    where TDomainEntity : EntityBase<TDomainEntity>
{
    public Id<TDomainEntity> Id { get; }

    protected EntityBase(Id<TDomainEntity> id)
    {
        ArgumentNullException.ThrowIfNull(id);

        Id = id;
    }
}


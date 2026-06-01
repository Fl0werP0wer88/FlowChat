using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public abstract class EntityBase<TDomainEntity>
    : IEntity<TDomainEntity>, IAuditableEntity
    where TDomainEntity : EntityBase<TDomainEntity>
{
    public Id<TDomainEntity> Id { get; }
    public string CreatedBy { get; private set; } = string.Empty;
    public UtcDateTimeOffset CreatedAtUtc { get; private set; } = null!;
    public string LastModifiedBy { get; private set; } = string.Empty;
    public UtcDateTimeOffset LastModifiedAtUtc { get; private set; } = null!;

    protected EntityBase(Id<TDomainEntity> id)
    {
        ArgumentNullException.ThrowIfNull(id);

        Id = id;
    }

    public void SetCreated(string createdBy)
    {
        ArgumentNullException.ThrowIfNull(createdBy);

        CreatedBy = createdBy;
        CreatedAtUtc = UtcDateTimeOffset.UtcNow;
    }

    public void SetUpdated(string lastModifiedBy)
    {
        ArgumentNullException.ThrowIfNull(lastModifiedBy);

        LastModifiedBy = lastModifiedBy;
        LastModifiedAtUtc = UtcDateTimeOffset.UtcNow;
    }
}


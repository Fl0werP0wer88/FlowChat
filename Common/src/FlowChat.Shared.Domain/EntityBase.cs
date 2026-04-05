using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public abstract class EntityBase<TDomainEntity>
    : IEntity<TDomainEntity>, IAuditableEntity
    where TDomainEntity : EntityBase<TDomainEntity>
{
    public Id<TDomainEntity> Id { get; }
    public int Version { get; private set; } = 1;
    public string CreatedBy { get; private set; } = string.Empty;
    public UtcDateTimeOffset CreatedAtUtc { get; private set; }
    public string LastModifiedBy { get; private set; } = string.Empty;
    public UtcDateTimeOffset LastModifiedAtUtc { get; private set; }

    protected EntityBase() : this(Id<TDomainEntity>.New()) { }
    protected EntityBase(Id<TDomainEntity>? id)
    {
        Id = id ?? Id<TDomainEntity>.New();
        CreatedAtUtc = UtcDateTimeOffset.UtcNow;
        LastModifiedAtUtc = UtcDateTimeOffset.UtcNow;
    }

    public void IncrementVersion()
    {
        Version++;
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


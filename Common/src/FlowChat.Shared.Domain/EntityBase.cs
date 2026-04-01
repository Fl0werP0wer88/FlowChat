namespace FlowChat.Shared.Domain;

public abstract class EntityBase<TDomainEntity>
    : IEntity<TDomainEntity>, IAuditableEntity
    where TDomainEntity : EntityBase<TDomainEntity>
{
    public Id<TDomainEntity> Id { get; }
    public int Version { get; private set; } = 1;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string LastModifiedBy { get; private set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; private set; }

    protected EntityBase() : this(Id<TDomainEntity>.New()) { }
    protected EntityBase(Id<TDomainEntity>? id)
    {
        Id = id ?? Id<TDomainEntity>.New();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public void IncrementVersion()
    {
        Version++;
    }

    public void SetCreated(string createdBy)
    {
        ArgumentNullException.ThrowIfNull(createdBy);

        CreatedBy = createdBy;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetUpdated(string lastModifiedBy)
    {
        ArgumentNullException.ThrowIfNull(lastModifiedBy);

        LastModifiedBy = lastModifiedBy;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }
}


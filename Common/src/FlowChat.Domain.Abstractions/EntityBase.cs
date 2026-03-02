namespace FlowChat.Domain.Abstractions;

public abstract class EntityBase<TDomainEntity>
    where TDomainEntity : EntityBase<TDomainEntity>
{
    public Id<TDomainEntity> Id { get; }
    public string CreatedBy { get; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; }
    public string LastModifiedBy { get; private set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; private set; }

    protected EntityBase() : this(Id<TDomainEntity>.New()) { }
    protected EntityBase(Id<TDomainEntity>? id)
    {
        Id = id ?? Id<TDomainEntity>.New();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }

    protected void Modified(string modifier)
    {
        LastModifiedBy = modifier;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }
}

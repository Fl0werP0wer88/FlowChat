namespace FlowChat.Domain.Abstractions;

public abstract class AuditableEntityBase : IAuditableEntity
{
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string LastModifiedBy { get; private set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; private set; }

    protected AuditableEntityBase()
    {
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
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

namespace FlowChat.Shared.Persistance;

public abstract class AuditableReadEntityBase : ReadEntityBase, IPersistenceAuditableEntity
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}

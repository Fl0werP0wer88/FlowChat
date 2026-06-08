namespace FlowChat.Shared.Persistance;

public abstract class ReadModelEntityBase : AuditableReadEntityBase
{
    public int SourceVersion { get; set; }
    public string SourceCreatedBy { get; set; } = string.Empty;
    public DateTimeOffset SourceCreatedAtUtc { get; set; }
    public string SourceLastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset SourceLastModifiedAtUtc { get; set; }
    public string SourceDeletedBy { get; set; } = string.Empty;
    public DateTimeOffset SourceDeletedAtUtc { get; set; }
    public override DateTimeOffset? DeletedAt => SourceDeletedAtUtc == default ? null : SourceDeletedAtUtc;

}

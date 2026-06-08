namespace FlowChat.Shared.Persistance;

public abstract class ReadModelEntityBase : AuditableReadEntityBase
{
    public int SourceVersion { get; set; }
    public DateTimeOffset SourceCreatedAtUtc { get; set; }
    public DateTimeOffset SourceLastModifiedAtUtc { get; set; }
    public DateTimeOffset? SourceDeletedAtUtc { get; set; }
    public override DateTimeOffset? DeletedAt => SourceDeletedAtUtc;
}

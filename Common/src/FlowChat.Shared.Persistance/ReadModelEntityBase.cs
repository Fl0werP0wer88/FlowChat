namespace FlowChat.Shared.Persistance;

public abstract class ReadModelEntityBase : AuditableReadEntityBase
{
    public int SourceVersion { get; set; }
}

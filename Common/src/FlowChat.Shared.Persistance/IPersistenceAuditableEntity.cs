namespace FlowChat.Shared.Persistance;

public interface IPersistenceAuditableEntity
{
    string CreatedBy { get; set; }
    DateTimeOffset CreatedAtUtc { get; set; }
    string LastModifiedBy { get; set; }
    DateTimeOffset LastModifiedAtUtc { get; set; }
}

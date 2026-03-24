namespace FlowChat.Domain.Abstractions;

public interface IAuditableEntity
{
    string CreatedBy { get; }
    DateTimeOffset CreatedAtUtc { get; }
    string LastModifiedBy { get; }
    DateTimeOffset LastModifiedAtUtc { get; }

    void SetCreated(string createdBy);
    void SetUpdated(string lastModifiedBy);
}

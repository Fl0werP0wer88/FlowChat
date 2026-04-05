using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public interface IAuditableEntity
{
    string CreatedBy { get; }
    UtcDateTimeOffset CreatedAtUtc { get; }
    string LastModifiedBy { get; }
    UtcDateTimeOffset LastModifiedAtUtc { get; }

    void SetCreated(string createdBy);
    void SetUpdated(string lastModifiedBy);
}


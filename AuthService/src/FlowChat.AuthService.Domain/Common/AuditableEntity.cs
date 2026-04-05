using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Domain.Common;

public class AuditableEntity
{
    public string CreatedBy { get; set; } = string.Empty;
    public UtcDateTimeOffset CreatedDate { get; set; } = UtcDateTimeOffset.UtcNow;
    public string LastModifiedBy { get; set; } = string.Empty;
    public UtcDateTimeOffset LastModifiedDate { get; set; } = UtcDateTimeOffset.UtcNow;
}


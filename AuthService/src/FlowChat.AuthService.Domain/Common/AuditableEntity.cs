namespace FlowChat.AuthService.Domain.Common;

public class AuditableEntity
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedDate { get; set; }
}


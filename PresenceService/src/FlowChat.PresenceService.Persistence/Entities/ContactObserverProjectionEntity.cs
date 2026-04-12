namespace FlowChat.PresenceService.Persistence.Entities;

public sealed class ContactObserverProjectionEntity
{
    public Guid ObservedUserId { get; set; }
    public Guid ObserverUserId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}

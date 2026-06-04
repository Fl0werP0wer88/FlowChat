using FlowChat.Shared.Persistance;

namespace FlowChat.PresenceService.Persistence.Entities;

public sealed class ContactObserverReadModelEntity : AuditableReadEntityBase
{
    public Guid ObservedUserId { get; set; }
    public Guid ObserverUserId { get; set; }
    public int SourceVersion { get; set; }
}

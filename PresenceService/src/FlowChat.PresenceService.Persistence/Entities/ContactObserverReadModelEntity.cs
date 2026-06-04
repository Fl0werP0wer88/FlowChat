using FlowChat.Shared.Persistance;

namespace FlowChat.PresenceService.Persistence.Entities;

public sealed class ContactObserverReadModelEntity : ReadModelEntityBase
{
    public Guid ObservedUserId { get; set; }
    public Guid ObserverUserId { get; set; }
}

using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Persistence.Entities;

public sealed class UserPresencePreferencesEntity
{
    public Guid UserId { get; set; }
    public PresenceStatus PreferredStatus { get; set; }
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}

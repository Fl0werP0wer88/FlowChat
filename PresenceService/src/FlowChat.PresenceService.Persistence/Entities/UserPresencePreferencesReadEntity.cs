using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Persistence.Entities;

public sealed class UserPresencePreferencesReadEntity
{
    public Guid UserId { get; init; }
    public PresenceStatus PreferredStatus { get; init; }
    public DateTimeOffset LastModifiedAtUtc { get; init; }
}

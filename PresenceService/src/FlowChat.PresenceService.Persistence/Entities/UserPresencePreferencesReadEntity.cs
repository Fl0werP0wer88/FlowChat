using FlowChat.Core.Domain;
using FlowChat.Shared.Persistance;

namespace FlowChat.PresenceService.Persistence.Entities;

public sealed class UserPresencePreferencesReadEntity : ReadEntityBase
{
    public Guid UserId { get; init; }
    public PresenceStatus PreferredStatus { get; init; }
    public DateTimeOffset LastModifiedAtUtc { get; init; }
}

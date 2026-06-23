using FlowChat.Core.Domain;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;

public sealed class UserPresencePreferences : AggregateRootBase<UserPresencePreferences>
{
    public PresenceStatus PreferredStatus { get; private set; }

    public Guid UserId => Id.Value;

    private UserPresencePreferences(
        Id<UserPresencePreferences> id,
        PresenceStatus preferredStatus) : base(id)
    {
        SetPreferredStatus(preferredStatus);
    }

    public static UserPresencePreferences Create(Guid userId, PresenceStatus preferredStatus)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        return new UserPresencePreferences(Id<UserPresencePreferences>.FromGuid(userId), preferredStatus);
    }

    public static UserPresencePreferences Restore(
        Id<UserPresencePreferences> id,
        PresenceStatus preferredStatus)
    {
        return new UserPresencePreferences(id, preferredStatus);
    }

    public void SetPreferredStatus(PresenceStatus preferredStatus)
    {
        if (preferredStatus is not (PresenceStatus.Busy or PresenceStatus.Invisible))
        {
            throw new ArgumentException("Only manual presence statuses can be saved as preferences.", nameof(preferredStatus));
        }

        PreferredStatus = preferredStatus;
    }
}

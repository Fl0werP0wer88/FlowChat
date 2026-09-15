using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.ValueObjects;

public sealed class DuetParticipantPair : IEquatable<DuetParticipantPair>
{
    public Id<UserProfileMarker> FirstUserId { get; }
    public Id<UserProfileMarker> SecondUserId { get; }

    private DuetParticipantPair(
        Id<UserProfileMarker> firstUserId,
        Id<UserProfileMarker> secondUserId)
    {
        FirstUserId = firstUserId;
        SecondUserId = secondUserId;
    }

    public static DuetParticipantPair Create(
        Id<UserProfileMarker> firstUserId,
        Id<UserProfileMarker> secondUserId)
    {
        ArgumentNullException.ThrowIfNull(firstUserId);
        ArgumentNullException.ThrowIfNull(secondUserId);

        if (firstUserId == secondUserId)
        {
            throw new ArgumentException(
                "One-on-one conversations require two distinct participants.",
                nameof(secondUserId));
        }

        return firstUserId.Value < secondUserId.Value
            ? new DuetParticipantPair(firstUserId, secondUserId)
            : new DuetParticipantPair(secondUserId, firstUserId);
    }

    public bool Equals(DuetParticipantPair? other) =>
        other is not null &&
        FirstUserId == other.FirstUserId &&
        SecondUserId == other.SecondUserId;

    public override bool Equals(object? obj) => obj is DuetParticipantPair other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(FirstUserId, SecondUserId);

    public static bool operator ==(DuetParticipantPair? left, DuetParticipantPair? right) => Equals(left, right);

    public static bool operator !=(DuetParticipantPair? left, DuetParticipantPair? right) => !Equals(left, right);
}

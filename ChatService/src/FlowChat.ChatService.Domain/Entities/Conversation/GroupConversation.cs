using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class GroupConversation : Conversation, IEntity<GroupConversation>
{
    private const int MinimumParticipantsCount = 2;
    private const string MinimumParticipantsErrorMessage = "Group conversations must have at least two participants.";

    Id<GroupConversation> IEntity<GroupConversation>.Id => Id<GroupConversation>.FromId(Id);

    private GroupConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId) : base(id, type, name, createdByUserId)
    {
    }

    private GroupConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        List<ParticipantUser> participants) : base(id, type, name, createdByUserId, participants)
    {
    }

    public static GroupConversation Create(
        Id<Conversation> id,
        Id<UserProfileMarker> createdByUserId,
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string name)
    {
        var conversation = CreateCore(
            id,
            ConversationType.Group,
            createdByUserId,
            participantUserIds.Prepend(createdByUserId),
            name,
            static (id, type, name, createdByUserId, participants) =>
                new GroupConversation(id, type, name, createdByUserId, participants));

        conversation.AddDomainEvent(new GroupConversationCreatedDomainEvent(
            conversation.Id,
            conversation.Type,
            conversation.Name,
            conversation.CreatedByUserId));
        return conversation;
    }

    public void AddParticipants(
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string? displayName = null,
        long lastReadMessageSequenceNum = 0)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var userIdsToAdd = participantUserIds.ToList();

        if (userIdsToAdd.Count == 0)
            throw new ArgumentException("At least one participant must be provided.", nameof(participantUserIds));

        foreach (var participantUserId in userIdsToAdd)
        {
            ArgumentNullException.ThrowIfNull(participantUserId);

            if (_participants.Any(p => p.UserId == participantUserId) || userIdsToAdd.Count(id => id == participantUserId) > 1)
                throw new InvalidOperationException("User is already a participant in this conversation.");
        }

        if (_participants.Count + userIdsToAdd.Count < MinimumParticipantsCount)
            throw new InvalidOperationException(MinimumParticipantsErrorMessage);

        foreach (var participantUserId in userIdsToAdd)
        {
            _participants.Add(ParticipantUser.Create(
                Id<ParticipantUser>.New(),
                Id,
                participantUserId,
                displayName,
                lastReadMessageSequenceNum));
        }

        MembershipRevision++;
    }

    public void RemoveParticipants(IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var userIdsToRemove = participantUserIds.ToList();

        if (userIdsToRemove.Count == 0)
            throw new ArgumentException("At least one participant must be provided.", nameof(participantUserIds));

        foreach (var participantUserId in userIdsToRemove)
        {
            ArgumentNullException.ThrowIfNull(participantUserId);

            if (_participants.All(p => p.UserId != participantUserId))
                throw new InvalidOperationException("User is not a participant in this conversation.");
        }

        if (_participants.Count - userIdsToRemove.Distinct().Count() < MinimumParticipantsCount)
            throw new InvalidOperationException(MinimumParticipantsErrorMessage);

        _participants.RemoveAll(p => userIdsToRemove.Contains(p.UserId));

        MembershipRevision++;
    }
}

using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class GroupConversation : Conversation
{
    private const int MinimumParticipantsCount = 2;
    private const string MinimumParticipantsErrorMessage = "Group conversations must have at least two participants.";

    private GroupConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum) : base(id, type, name, createdByUserId, lastMsgSequenceNum)
    {
    }

    private GroupConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum,
        List<ParticipantUser> participants) : base(id, type, name, createdByUserId, lastMsgSequenceNum, participants)
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
            static (id, type, name, createdByUserId, lastMsgSequenceNum, participants) =>
                new GroupConversation(id, type, name, createdByUserId, lastMsgSequenceNum, participants));

        conversation.AddDomainEvent(new GroupConversationCreatedDomainEvent(
            conversation.Id,
            conversation.Type,
            conversation.Name,
            conversation.CreatedByUserId));

        conversation.AddDomainEvent(new GroupConversationParticipantsAddedDomainEvent(
            conversation.Id,
            [.. conversation.Participants.Select(p => p.UserId)]));

        return conversation;
    }

    public void AddParticipants(
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        string? displayName = null,
        string? avatarUrl = null,
        long lastReadMessageSequenceNum = 0)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var userIdsToAdd = participantUserIds.ToList();

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
                avatarUrl,
                lastReadMessageSequenceNum));
        }

        AddDomainEvent(new GroupConversationParticipantsAddedDomainEvent(Id, userIdsToAdd));
    }

    public void RemoveParticipants(IEnumerable<Id<UserProfileMarker>> participantUserIds)
    {
        ArgumentNullException.ThrowIfNull(participantUserIds);

        var userIdsToRemove = participantUserIds.ToList();

        foreach (var participantUserId in userIdsToRemove)
        {
            ArgumentNullException.ThrowIfNull(participantUserId);

            if (_participants.All(p => p.UserId != participantUserId))
                throw new InvalidOperationException("User is not a participant in this conversation.");
        }

        if (_participants.Count - userIdsToRemove.Distinct().Count() < MinimumParticipantsCount)
            throw new InvalidOperationException(MinimumParticipantsErrorMessage);

        _participants.RemoveAll(p => userIdsToRemove.Contains(p.UserId));

        AddDomainEvent(new GroupConversationParticipantsRemovedDomainEvent(Id, userIdsToRemove));
    }
}

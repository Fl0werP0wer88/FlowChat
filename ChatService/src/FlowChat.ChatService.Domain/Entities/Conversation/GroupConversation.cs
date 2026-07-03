using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class GroupConversation : Conversation
{
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
            participantUserIds,
            name,
            static (id, type, name, createdByUserId, lastMsgSequenceNum, participants) =>
                new GroupConversation(id, type, name, createdByUserId, lastMsgSequenceNum, participants));

        conversation.AddDomainEvent(new GroupConversationCreatedDomainEvent(
            conversation.Id,
            conversation.Type,
            conversation.Name,
            conversation.CreatedByUserId,
            [.. conversation.Participants.Select(p => p.UserId)]));

        return conversation;
    }

    public static GroupConversation Restore(
        Id<Conversation> id,
        string name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum,
        IEnumerable<ParticipantUser> participants)
    {
        return RestoreCore(
            id,
            ConversationType.Group,
            name,
            createdByUserId,
            lastMsgSequenceNum,
            participants,
            static (id, type, name, createdByUserId, lastMsgSequenceNum, participants) =>
                new GroupConversation(id, type, name, createdByUserId, lastMsgSequenceNum, participants));
    }

    public void AddParticipant(
        Id<UserProfileMarker> participantUserId,
        string? displayName = null,
        string? avatarUrl = null,
        long lastReadMessageSequenceNum = 0)
    {
        AddParticipantCore(participantUserId, displayName, avatarUrl, lastReadMessageSequenceNum);
    }
}

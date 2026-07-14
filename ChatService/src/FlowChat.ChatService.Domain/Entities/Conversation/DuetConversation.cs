using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class DuetConversation : Conversation, IEntity<DuetConversation>
{
    Id<DuetConversation> IEntity<DuetConversation>.Id => Id<DuetConversation>.FromId(Id);

    private DuetConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum) : base(id, type, name, createdByUserId, lastMsgSequenceNum)
    {
    }

    private DuetConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum,
        List<ParticipantUser> participants) : base(id, type, name, createdByUserId, lastMsgSequenceNum, participants)
    {
    }

    public static DuetConversation Create(
        Id<UserProfileMarker> createdByUserId,
        Id<UserProfileMarker> partnerUserId)
    {
        return CreateCore(
            Id<Conversation>.New(),
            ConversationType.Duet,
            createdByUserId,
            [createdByUserId, partnerUserId],
            name: null,
            static (id, type, name, createdByUserId, lastMsgSequenceNum, participants) =>
                new DuetConversation(id, type, name, createdByUserId, lastMsgSequenceNum, participants));
    }

    public static DuetConversation Restore(
        Id<Conversation> id,
        Id<UserProfileMarker> createdByUserId,
        long lastMsgSequenceNum,
        IEnumerable<ParticipantUser> participants)
    {
        return RestoreCore(
            id,
            ConversationType.Duet,
            name: null,
            createdByUserId,
            lastMsgSequenceNum,
            participants,
            static (id, type, name, createdByUserId, lastMsgSequenceNum, participants) =>
                new DuetConversation(id, type, name, createdByUserId, lastMsgSequenceNum, participants));
    }

    public (Id<UserProfileMarker> FirstUserId, Id<UserProfileMarker> SecondUserId) GetParticipantPair()
    {
        var participantUserIds = Participants.Select(participant => participant.UserId).ToArray();

        return (participantUserIds[0], participantUserIds[1]);
    }
}

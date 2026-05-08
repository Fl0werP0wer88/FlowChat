using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class GroupConversation : Conversation
{
    private GroupConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId) : base(id, type, name, createdByUserId)
    {
    }

    private GroupConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId,
        List<ParticipantUser> participants) : base(id, type, name, createdByUserId, participants)
    {
    }

    public static GroupConversation Create(
        Guid createdByUserId,
        IEnumerable<Guid> participantUserIds,
        string name)
    {
        return CreateCore(
            Id<Conversation>.New(),
            ConversationType.Group,
            createdByUserId,
            participantUserIds,
            name,
            static (id, type, name, createdByUserId, participants) =>
                new GroupConversation(id, type, name, createdByUserId, participants));
    }

    public static GroupConversation Restore(
        Id<Conversation> id,
        string name,
        Guid createdByUserId,
        IEnumerable<ParticipantUser> participants)
    {
        return RestoreCore(
            id,
            ConversationType.Group,
            name,
            createdByUserId,
            participants,
            static (id, type, name, createdByUserId, participants) =>
                new GroupConversation(id, type, name, createdByUserId, participants));
    }

    public void AddParticipant(Guid participantUserId, string? displayedName = null, string? avatarUrl = null)
    {
        AddParticipantCore(participantUserId, displayedName, avatarUrl);
    }
}

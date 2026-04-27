using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Entities.Conversation;

public sealed class DuetConversation : Conversation
{
    private DuetConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId) : base(id, type, name, createdByUserId)
    {
    }

    private DuetConversation(
        Id<Conversation> id,
        ConversationType type,
        string? name,
        Guid createdByUserId,
        List<ParticipantUser> participants) : base(id, type, name, createdByUserId, participants)
    {
    }

    public static DuetConversation Create(
        Guid createdByUserId,
        Guid partnerUserId)
    {
        return CreateCore(
            Id<Conversation>.New(),
            ConversationType.Duet,
            createdByUserId,
            [createdByUserId, partnerUserId],
            name: null,
            static (id, type, name, createdByUserId, participants) =>
                new DuetConversation(id, type, name, createdByUserId, participants));
    }

    public static DuetConversation Restore(
        Id<Conversation> id,
        Guid createdByUserId,
        IEnumerable<ParticipantUser> participants)
    {
        return RestoreCore(
            id,
            ConversationType.Duet,
            name: null,
            createdByUserId,
            participants,
            static (id, type, name, createdByUserId, participants) =>
                new DuetConversation(id, type, name, createdByUserId, participants));
    }
}

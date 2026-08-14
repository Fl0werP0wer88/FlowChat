using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationMembershipReadEntityV2 : ReadEntityBase
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public int ConversationType { get; init; }
    public int ParticipantCount { get; init; }
    public int Version { get; init; }
}

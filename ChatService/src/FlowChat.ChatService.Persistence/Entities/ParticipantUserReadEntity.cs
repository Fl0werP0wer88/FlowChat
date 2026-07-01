using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ParticipantUserReadEntity : ReadEntityBase
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public long LastReadMessageSequenceNum { get; init; }
}

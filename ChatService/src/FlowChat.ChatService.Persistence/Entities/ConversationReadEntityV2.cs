using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationReadEntityV2 : ReadEntityBase
{
    public Guid Id { get; init; }
    public int ConversationType { get; init; }
    public string? Name { get; init; }
    public int Version { get; init; }
}

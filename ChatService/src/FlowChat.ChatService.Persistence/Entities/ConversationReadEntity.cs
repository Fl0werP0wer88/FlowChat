using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationReadEntity : ReadEntityBase
{
    public Guid Id { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
    public long LastMsgSequenceNum { get; init; }
    public int Version { get; init; }
    public int MembershipRevision { get; init; }
}

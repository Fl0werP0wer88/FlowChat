namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class ConversationChangedDto
{
    public Guid ConversationId { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
    public Guid CreatedByUserId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}

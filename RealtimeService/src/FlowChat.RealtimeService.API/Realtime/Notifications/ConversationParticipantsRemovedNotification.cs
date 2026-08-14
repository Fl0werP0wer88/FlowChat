namespace FlowChat.RealtimeService.Api.Realtime.Notifications;

public sealed class ConversationParticipantsRemovedNotification
{
    public Guid ConversationId { get; init; }
    public int ConversationType { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}

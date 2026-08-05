namespace FlowChat.RealtimeService.Api.Realtime.Notifications;

public sealed class ConversationParticipantsAddedNotification
{
    public Guid ConversationId { get; init; }
    public int ConversationType { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}

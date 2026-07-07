namespace FlowChat.RealtimeService.Api.Realtime.Notifications;

public sealed class GroupConversationParticipantsAddedNotification
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}

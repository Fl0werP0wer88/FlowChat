namespace FlowChat.RealtimeService.Api.Realtime.Notifications;

public sealed class GroupConversationChangedNotification
{
    public Guid ConversationId { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
}

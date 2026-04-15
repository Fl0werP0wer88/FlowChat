namespace FlowChat.RealtimeService.Api.Realtime;

public interface IRealtimeClient
{
    Task ReceiveMessage(ChatMessageNotificationDto payload);

    Task PresenceChanged(PresenceChangedNotificationDto payload);

    Task ReceiveContactPresenceStatuses(IReadOnlyCollection<PresenceChangedNotificationDto> statuses);
}

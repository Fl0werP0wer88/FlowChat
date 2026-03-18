namespace FlowChat.RealtimeService.Infrastructure.Realtime;

public interface IRealtimeClient
{
    Task ReceiveMessage(ChatMessageNotificationDto payload);

    Task PresenceChanged(PresenceChangedNotificationDto payload);
}

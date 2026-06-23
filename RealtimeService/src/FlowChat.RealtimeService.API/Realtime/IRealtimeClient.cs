namespace FlowChat.RealtimeService.Api.Realtime;

public interface IRealtimeClient
{
    Task ReceiveMessage(ChatMessageNotificationDto payload);

    Task PresenceChanged(PresenceDto payload);
}

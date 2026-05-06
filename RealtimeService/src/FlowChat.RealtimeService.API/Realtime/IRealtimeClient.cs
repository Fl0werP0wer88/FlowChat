namespace FlowChat.RealtimeService.Api.Realtime;

public interface IRealtimeClient
{
    Task ReceiveMessage(ChatMessageNotificationDto payload);

    Task PresenceChanged(PresenceDto payload);

    Task ReceiveContactPresenceStatuses(IReadOnlyCollection<PresenceDto> statuses);

    Task ReceivePresencePreferences(PresencePreferencesDto preferences);
}

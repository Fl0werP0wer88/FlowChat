using FlowChat.RealtimeService.Api.Realtime.Notifications;

namespace FlowChat.RealtimeService.Api.Realtime;

public interface IRealtimeClient
{
    Task MessageReceived(ChatMessageReceivedNotification payload);

    Task PresenceChanged(PresenceChangedNotification payload);

    Task GroupConversationChanged(GroupConversationChangedNotification payload);
}

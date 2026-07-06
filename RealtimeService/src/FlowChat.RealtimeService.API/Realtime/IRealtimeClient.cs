namespace FlowChat.RealtimeService.Api.Realtime;

public interface IRealtimeClient
{
    Task MessageReceived(ChatMessageReceivedDto payload);

    Task PresenceChanged(PresenceDto payload);

    Task GroupConversationChanged(GroupConversationChangedDto payload);
}

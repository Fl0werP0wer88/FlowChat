using FlowChat.RealtimeService.Worker.Kafka;

namespace FlowChat.RealtimeService.Worker.Configuration;

public interface IWorkerSettingsManager
{
    ChatMessageSentConsumerOptions GetChatMessageSentConsumerOptions();

    UserPresenceChangedConsumerOptions GetUserPresenceChangedConsumerOptions();

    RealtimeApiSettings GetRealtimeApiSettings();
}

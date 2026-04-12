using FlowChat.RealtimeService.Consumers.Kafka;

namespace FlowChat.RealtimeService.Consumers.Configuration;

public interface IConsumersSettingsManager
{
    ChatMessageSentConsumerOptions GetChatMessageSentConsumerOptions();

    PresenceStatusChangedConsumerOptions GetPresenceStatusChangedConsumerOptions();

    RealtimeApiSettings GetRealtimeApiSettings();
}

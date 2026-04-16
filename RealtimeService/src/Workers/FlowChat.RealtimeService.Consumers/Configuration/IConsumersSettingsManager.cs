using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Routing.Configuration;

namespace FlowChat.RealtimeService.Consumers.Configuration;

public interface IConsumersSettingsManager
{
    ChatMessageSentConsumerOptions GetChatMessageSentConsumerOptions();

    PresenceStatusChangedConsumerOptions GetPresenceStatusChangedConsumerOptions();

    RealtimeApiSettings GetRealtimeApiSettings();

    RealtimeRoutingSettings GetRealtimeRoutingSettings();
}

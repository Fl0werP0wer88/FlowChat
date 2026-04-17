using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Routing.Configuration;

namespace FlowChat.RealtimeService.Consumers.Configuration;

public interface IConsumersSettingsManager
{
    ChatMessageSentConsumerSettingsSection GetChatMessageSentConsumerSettingsSection();

    PresenceStatusChangedConsumerSettingsSection GetPresenceStatusChangedConsumerSettingsSection();

    RealtimeApiSettingsSection GetRealtimeApiSettingsSection();

    RealtimeRoutingSettingsSection GetRealtimeRoutingSettingsSection();
}

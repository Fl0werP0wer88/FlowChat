using FlowChat.ChatService.Persistence.Configuration;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public interface IWorkerSettingsManager
{
    ChatMessageSentProducerSettingsSection GetChatMessageSentProducerSettingsSection();
    OutboxPublisherRuntimeSettingsSection GetOutboxPublisherRuntimeSettingsSection();
}

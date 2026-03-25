using FlowChat.ChatService.Persistence.Configuration;

namespace FlowChat.ChatService.Worker.Kafka;

public interface IWorkerSettingsManager
{
    ChatMessageSentProducerOptions GetChatMessageSentProducerOptions();
    OutboxPublisherRuntimeOptions GetOutboxPublisherRuntimeOptions();
}

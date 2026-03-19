using FlowChat.ChatService.Persistence.Configuration;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public interface IWorkerSettingsManager
{
    ChatMessageSentProducerOptions GetChatMessageSentProducerOptions();
    OutboxPublisherRuntimeOptions GetOutboxPublisherRuntimeOptions();
}

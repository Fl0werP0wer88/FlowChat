using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public sealed class ChatMessageSentProducerOptions : IKafkaProducerOptions<ChatMessageSentIntegrationEvent>
{
    public const string SectionName = "Kafka:ChatMessageSentProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.chat.message.v1";
}

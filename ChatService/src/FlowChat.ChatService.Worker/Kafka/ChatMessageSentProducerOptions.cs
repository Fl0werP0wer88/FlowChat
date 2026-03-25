namespace FlowChat.ChatService.Worker.Kafka;

public sealed class ChatMessageSentProducerOptions
{
    public const string SectionName = "Kafka:ChatMessageSentProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.chat.message.v1";
}

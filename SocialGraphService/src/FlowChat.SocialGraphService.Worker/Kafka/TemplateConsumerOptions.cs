namespace FlowChat.SocialGraphService.Worker.Kafka;

public sealed class TemplateConsumerOptions
{
    public const string SectionName = "Kafka:TemplateConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "socialgraph-service";
    public string Topic { get; set; } = "flowchat.socialgraph.events.v1";
}

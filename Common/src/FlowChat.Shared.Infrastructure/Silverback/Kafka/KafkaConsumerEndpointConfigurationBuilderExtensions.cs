using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public static class KafkaConsumerEndpointConfigurationBuilderExtensions
{
    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatEndpointDefaults(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string topic) =>
        endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
            .IgnoreUnhandledMessages();
}

using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

public static class TieredKafkaConsumerEndpointConfigurationBuilderExtensions
{
    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatTieredMainEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        ITieredRetryKafkaConsumerSettingsSection settings) =>
        endpoint
            .ConfigureFlowChatEndpointDefaults(settings.Topic)
            .OnError(new AtomicKafkaMoveErrorPolicy(settings, null));

    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatTieredRetryEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        ITieredRetryKafkaConsumerSettingsSection settings,
        int tierIndex) =>
        endpoint
            .ConfigureFlowChatEndpointDefaults(settings.RetryTiers[tierIndex].Topic)
            .OnError(new AtomicKafkaMoveErrorPolicy(settings, tierIndex));
}

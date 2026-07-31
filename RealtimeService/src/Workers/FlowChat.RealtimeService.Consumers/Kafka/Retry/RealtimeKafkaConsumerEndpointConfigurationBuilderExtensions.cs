using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.RealtimeService.Consumers.Kafka.Retry;

public static class RealtimeKafkaConsumerEndpointConfigurationBuilderExtensions
{
    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureRealtimeMainEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        ITieredRetryKafkaConsumerSettingsSection settings) =>
        endpoint
            .ConfigureFlowChatEndpointDefaults(settings.Topic)
            .OnError(new AtomicKafkaMoveErrorPolicy(settings, null));

    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureRealtimeRetryEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        ITieredRetryKafkaConsumerSettingsSection settings,
        int tierIndex) =>
        endpoint
            .ConfigureFlowChatEndpointDefaults(settings.RetryTiers[tierIndex].Topic)
            .OnError(new AtomicKafkaMoveErrorPolicy(settings, tierIndex));
}

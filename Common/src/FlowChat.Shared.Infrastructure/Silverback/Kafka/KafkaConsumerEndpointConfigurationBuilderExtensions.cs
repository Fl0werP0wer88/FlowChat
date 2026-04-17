using FlowChat.Core.Exceptions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public static class KafkaConsumerEndpointConfigurationBuilderExtensions
{
    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatMainEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        IRetryableKafkaConsumerSettingsSection options) =>
        ConfigureFlowChatEndpointDefaults(endpoint, options.Topic)
            .OnError(policy =>
            {
                policy.MoveTo(options.DeadLetterTopic, move => move
                    .ApplyTo<NonTransientException>());

                policy.MoveTo(options.RetryTopic, move => move
                    .Exclude<NonTransientException>());
            });

    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatRetryEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        IRetryableKafkaConsumerSettingsSection options) =>
        ConfigureFlowChatEndpointDefaults(endpoint, options.RetryTopic)
            .OnError(policy =>
            {
                policy.MoveTo(options.DeadLetterTopic, move => move
                    .ApplyTo<NonTransientException>());

                policy.Retry(retry => retry
                        .WithMaxRetries(options.MaxRetryCount)
                        .Exclude<NonTransientException>()
                        .WithExponentialDelay(
                            TimeSpan.FromSeconds(options.RetryBaseDelaySeconds),
                            2,
                            TimeSpan.FromSeconds(options.RetryMaxDelaySeconds)))
                    .ThenMoveTo(options.DeadLetterTopic, move => move
                        .Exclude<NonTransientException>());
            });

    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatEndpointDefaults(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string topic) =>
        endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
            .IgnoreUnhandledMessages();
}

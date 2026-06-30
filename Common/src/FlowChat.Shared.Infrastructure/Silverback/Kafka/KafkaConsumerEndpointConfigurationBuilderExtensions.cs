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
                // Only TransientException and IsolableException are eligible for retry; everything else (including unknown exceptions) goes straight to DLQ.
                policy.MoveTo(options.DeadLetterTopic, move => move
                    .Exclude<TransientException>()
                    .Exclude<IsolableException>());

                policy.MoveTo(options.RetryTopic, move => move
                    .ApplyTo<TransientException>()
                    .ApplyTo<IsolableException>());
            });

    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatRetryEndpoint(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        IRetryableKafkaConsumerSettingsSection options) =>
        ConfigureFlowChatEndpointDefaults(endpoint, options.RetryTopic)
            .OnError(policy =>
            {
                // Deliberately not extended to ApplyTo<IsolableException>: messages are already consumed
                // one-by-one here, so an isolable failure goes straight to the DLQ on first attempt instead
                // of wasting backoff retries on data that will never become valid.
                policy.MoveTo(options.DeadLetterTopic, move => move
                    .Exclude<TransientException>());

                policy.Retry(retry => retry
                        .WithMaxRetries(options.MaxRetryCount)
                        .ApplyTo<TransientException>()
                        .WithExponentialDelay(
                            TimeSpan.FromSeconds(options.RetryBaseDelaySeconds),
                            2,
                            TimeSpan.FromSeconds(options.RetryMaxDelaySeconds)))
                    .ThenMoveTo(options.DeadLetterTopic, move => move
                        .ApplyTo<TransientException>());
            });

    public static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureFlowChatEndpointDefaults(
        this KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string topic) =>
        endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
            .IgnoreUnhandledMessages();
}

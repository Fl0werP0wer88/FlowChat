using Confluent.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using Silverback.Messaging.Broker;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

public sealed class KafkaRetryPartitionController : IKafkaRetryPartitionController
{
    public void Pause(IConsumer consumer, TopicPartition topicPartition) =>
        GetKafkaConsumer(consumer).Pause([topicPartition]);

    public bool IsAssigned(IConsumer consumer, TopicPartition topicPartition) =>
        GetKafkaConsumer(consumer).Client.Assignment.Contains(topicPartition);

    public void Resume(IConsumer consumer, TopicPartition topicPartition) =>
        GetKafkaConsumer(consumer).Resume([topicPartition]);

    private static KafkaConsumer GetKafkaConsumer(IConsumer consumer) =>
        consumer as KafkaConsumer
        ?? throw new InvalidOperationException("Tiered retry delay can only be used with a Kafka consumer.");
}

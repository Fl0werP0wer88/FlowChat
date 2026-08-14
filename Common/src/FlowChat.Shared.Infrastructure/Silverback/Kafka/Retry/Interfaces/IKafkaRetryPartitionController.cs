using Confluent.Kafka;
using Silverback.Messaging.Broker;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

public interface IKafkaRetryPartitionController
{
    void Pause(IConsumer consumer, TopicPartition topicPartition);

    bool IsAssigned(IConsumer consumer, TopicPartition topicPartition);

    void Resume(IConsumer consumer, TopicPartition topicPartition);
}

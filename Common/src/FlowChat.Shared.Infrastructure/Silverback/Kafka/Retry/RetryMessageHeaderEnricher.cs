using System.Globalization;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

public static class RetryMessageHeaderEnricher
{
    public static void CopyKafkaKey(IRawInboundEnvelope inboundEnvelope, IOutboundEnvelope outboundEnvelope)
    {
        var kafkaKey = (inboundEnvelope as IBrokerEnvelope)?.GetKafkaKey();
        if (!string.IsNullOrWhiteSpace(kafkaKey))
            outboundEnvelope.SetKafkaKey(kafkaKey);
    }

    public static void Enrich(
        IOutboundEnvelope outboundEnvelope,
        IRawInboundEnvelope inboundEnvelope,
        Exception exception,
        RetryDestination destination,
        DateTimeOffset now)
    {
        if (inboundEnvelope.BrokerMessageIdentifier is not KafkaOffset kafkaOffset)
            throw new InvalidOperationException("Atomic Kafka retry requires a Kafka offset.");

        outboundEnvelope.Headers.Remove(RetryMessageHeaders.InvalidRetryMetadata);

        AddIfMissing(outboundEnvelope, RetryMessageHeaders.FirstFailedAtUtc, now.ToString("O"));
        AddIfMissing(outboundEnvelope, RetryMessageHeaders.OriginalTopic, kafkaOffset.TopicPartition.Topic);
        AddIfMissing(
            outboundEnvelope,
            RetryMessageHeaders.OriginalPartition,
            kafkaOffset.TopicPartition.Partition.Value.ToString(CultureInfo.InvariantCulture));
        AddIfMissing(
            outboundEnvelope,
            RetryMessageHeaders.OriginalOffset,
            kafkaOffset.Offset.Value.ToString(CultureInfo.InvariantCulture));

        outboundEnvelope.Headers.AddOrReplace(
            RetryMessageHeaders.LastErrorType,
            exception.GetType().FullName ?? exception.GetType().Name);

        if (destination.IsDeadLetter)
        {
            outboundEnvelope.Headers.Remove(RetryMessageHeaders.RetryAtUtc);
            return;
        }

        outboundEnvelope.Headers.AddOrReplace(
            RetryMessageHeaders.RetryAttempt,
            destination.Attempt!.Value.ToString(CultureInfo.InvariantCulture));
        outboundEnvelope.Headers.AddOrReplace(
            RetryMessageHeaders.RetryAtUtc,
            now.Add(destination.Delay!.Value).ToString("O"));
    }

    private static void AddIfMissing(IOutboundEnvelope envelope, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(envelope.Headers.GetValue(name)))
            envelope.Headers.Add(name, value);
    }
}

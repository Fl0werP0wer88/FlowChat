using System.Diagnostics;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;

namespace FlowChat.RealtimeService.Consumers.Kafka.Retry;

public sealed record AtomicKafkaMoveErrorPolicy(
    ITieredRetryKafkaConsumerSettingsSection Settings,
    int? SourceRetryTierIndex) : IErrorPolicy
{
    public IErrorPolicyImplementation Build(IServiceProvider serviceProvider) =>
        new Implementation(
            Settings,
            SourceRetryTierIndex,
            serviceProvider.GetRequiredService<IUnitOfWork>(),
            serviceProvider.GetRequiredService<IConsumedOffsetCommitter>(),
            serviceProvider.GetRequiredService<IProducerCollection>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            serviceProvider.GetRequiredService<ILogger<AtomicKafkaMoveErrorPolicy>>());

    private sealed class Implementation(
        ITieredRetryKafkaConsumerSettingsSection settings,
        int? sourceRetryTierIndex,
        IUnitOfWork unitOfWork,
        IConsumedOffsetCommitter offsetCommitter,
        IProducerCollection producers,
        TimeProvider timeProvider,
        ILogger<AtomicKafkaMoveErrorPolicy> logger) : IErrorPolicyImplementation
    {
        public bool CanHandle(ConsumerPipelineContext context, Exception exception) =>
            exception is not OperationCanceledException;

        public async Task<bool> HandleErrorAsync(ConsumerPipelineContext context, Exception exception)
        {
            var destination = RetryFailureRouter.Resolve(settings, sourceRetryTierIndex, exception);
            var producer = producers.GetProducerForEndpoint(destination.Topic);

            await unitOfWork.ExecuteInTransactionAsync(
                async cancellationToken =>
                {
                    var outboundEnvelope = CreateOutboundEnvelope(context.Envelope, producer);
                    RetryMessageHeaderEnricher.Enrich(
                        outboundEnvelope,
                        context.Envelope,
                        exception,
                        destination,
                        timeProvider.GetUtcNow());

                    await producer.ProduceAsync(outboundEnvelope);
                    await offsetCommitter.CommitConsumedOffsetsAsync(cancellationToken);

                    return true;
                },
                CancellationToken.None);

            await context.TransactionManager.RollbackAsync(exception, commitConsumer: true);

            Activity.Current?.SetTag("retry.attempt", destination.Attempt);
            Activity.Current?.SetTag("retry.tier", destination.Delay?.TotalSeconds);
            Activity.Current?.SetTag("original.topic", settings.Topic);

            var sourceTopic = ((KafkaOffset)context.Envelope.BrokerMessageIdentifier).TopicPartition.Topic;
            if (destination.IsDeadLetter)
            {
                logger.LogWarning(
                    exception,
                    "Atomically moved Kafka message from {SourceTopic} to DLQ {DestinationTopic}.",
                    sourceTopic,
                    destination.Topic);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Atomically moved Kafka message from {SourceTopic} to retry topic {DestinationTopic} for attempt {RetryAttempt} after {Delay}.",
                    sourceTopic,
                    destination.Topic,
                    destination.Attempt,
                    destination.Delay);
            }

            return true;
        }

        private static IOutboundEnvelope CreateOutboundEnvelope(IRawInboundEnvelope inboundEnvelope, IProducer producer)
        {
            var outboundEnvelope = inboundEnvelope is IInboundEnvelope deserializedEnvelope
                ? OutboundEnvelopeFactory.CreateEnvelope(
                    deserializedEnvelope.Message,
                    deserializedEnvelope.Headers,
                    producer.EndpointConfiguration,
                    producer)
                : OutboundEnvelopeFactory.CreateEnvelope(
                    inboundEnvelope.RawMessage,
                    inboundEnvelope.Headers,
                    producer.EndpointConfiguration,
                    producer);

            RetryMessageHeaderEnricher.CopyKafkaKey(inboundEnvelope, outboundEnvelope);

            return outboundEnvelope;
        }
    }

}

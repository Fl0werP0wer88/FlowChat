using FlowChat.Core.Exceptions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka.Retry;

public sealed class RetryMessageHeaderEnricherTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CopyKafkaKey_InboundKafkaKey_PreservesKeyOnOutboundEnvelope()
    {
        var inboundHeaders = new MessageHeaderCollection();
        var inbound = new Mock<IRawInboundEnvelope>();
        var brokerInbound = inbound.As<IBrokerEnvelope>();
        brokerInbound.SetupGet(x => x.Headers).Returns(inboundHeaders);
        inboundHeaders.Add(KafkaMessageHeaders.MessageKey, "conversation-123");
        var outboundHeaders = new MessageHeaderCollection();
        var outbound = CreateOutboundEnvelope(outboundHeaders);

        RetryMessageHeaderEnricher.CopyKafkaKey(inbound.Object, outbound);

        outbound.GetKafkaKey().Should().Be("conversation-123");
    }

    [Fact]
    public void Enrich_FirstFailure_AddsRetryAndOriginalSourceHeaders()
    {
        var headers = new MessageHeaderCollection();
        headers.Add("event-id", "event-123");
        var outbound = CreateOutboundEnvelope(headers);
        var inbound = CreateInboundEnvelope("main-topic", 2, 42);
        var destination = new RetryDestination("retry-topic", TimeSpan.FromSeconds(5), 1, false);

        RetryMessageHeaderEnricher.Enrich(
            outbound,
            inbound,
            new TransientException("full exception message must stay out of headers"),
            destination,
            Now);

        headers.GetValue("event-id").Should().Be("event-123");
        headers.GetValue(RetryMessageHeaders.RetryAttempt).Should().Be("1");
        headers.GetValue(RetryMessageHeaders.RetryAtUtc).Should().Be(Now.AddSeconds(5).ToString("O"));
        headers.GetValue(RetryMessageHeaders.FirstFailedAtUtc).Should().Be(Now.ToString("O"));
        headers.GetValue(RetryMessageHeaders.OriginalTopic).Should().Be("main-topic");
        headers.GetValue(RetryMessageHeaders.OriginalPartition).Should().Be("2");
        headers.GetValue(RetryMessageHeaders.OriginalOffset).Should().Be("42");
        headers.GetValue(RetryMessageHeaders.LastErrorType).Should().Be(typeof(TransientException).FullName);
        headers.Select(header => header.Value).Should().NotContain("full exception message must stay out of headers");
    }

    [Fact]
    public void Enrich_NextRetry_PreservesFirstSourceAndUpdatesRetryMetadata()
    {
        var headers = new MessageHeaderCollection();
        headers.Add(RetryMessageHeaders.FirstFailedAtUtc, Now.AddMinutes(-1).ToString("O"));
        headers.Add(RetryMessageHeaders.OriginalTopic, "original-topic");
        headers.Add(RetryMessageHeaders.OriginalPartition, "4");
        headers.Add(RetryMessageHeaders.OriginalOffset, "123");
        var outbound = CreateOutboundEnvelope(headers);

        RetryMessageHeaderEnricher.Enrich(
            outbound,
            CreateInboundEnvelope("first-retry-topic", 0, 7),
            new TransientException("failure"),
            new RetryDestination("next-retry-topic", TimeSpan.FromSeconds(20), 2, false),
            Now);

        headers.GetValue(RetryMessageHeaders.FirstFailedAtUtc).Should().Be(Now.AddMinutes(-1).ToString("O"));
        headers.GetValue(RetryMessageHeaders.OriginalTopic).Should().Be("original-topic");
        headers.GetValue(RetryMessageHeaders.OriginalPartition).Should().Be("4");
        headers.GetValue(RetryMessageHeaders.OriginalOffset).Should().Be("123");
        headers.GetValue(RetryMessageHeaders.RetryAttempt).Should().Be("2");
        headers.GetValue(RetryMessageHeaders.RetryAtUtc).Should().Be(Now.AddSeconds(20).ToString("O"));
    }

    [Fact]
    public void Enrich_Dlq_RemovesRetryAtAndRecordsLastErrorType()
    {
        var headers = new MessageHeaderCollection();
        headers.Add(RetryMessageHeaders.RetryAtUtc, Now.AddSeconds(5).ToString("O"));
        var outbound = CreateOutboundEnvelope(headers);

        RetryMessageHeaderEnricher.Enrich(
            outbound,
            CreateInboundEnvelope("retry-topic", 0, 7),
            new NonTransientException("failure"),
            new RetryDestination("dlq-topic", null, null, true),
            Now);

        headers.GetValue(RetryMessageHeaders.RetryAtUtc).Should().BeNull();
        headers.GetValue(RetryMessageHeaders.LastErrorType).Should().Be(typeof(NonTransientException).FullName);
    }

    private static IOutboundEnvelope CreateOutboundEnvelope(MessageHeaderCollection headers)
    {
        var envelope = new Mock<IOutboundEnvelope>();
        envelope.SetupGet(x => x.Headers).Returns(headers);
        envelope.Setup(x => x.AddOrReplaceHeader(It.IsAny<string>(), It.IsAny<object?>()))
            .Callback<string, object?>((name, value) => headers.AddOrReplace(name, value))
            .Returns(envelope.Object);
        return envelope.Object;
    }

    private static IRawInboundEnvelope CreateInboundEnvelope(string topic, int partition, long offset)
    {
        var envelope = new Mock<IRawInboundEnvelope>();
        envelope.SetupGet(x => x.BrokerMessageIdentifier).Returns(new KafkaOffset(topic, partition, offset));
        return envelope.Object;
    }
}

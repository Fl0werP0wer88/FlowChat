using Confluent.Kafka;
using Moq;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace FlowChat.AuthService.UnitTests;

internal static class SilverbackTestEnvelopeExtensions
{
    public static IInboundEnvelope<TMessage> ToInboundEnvelope<TMessage>(
        this TMessage message,
        string sourceTopic = "dev.flowchat.test.v1")
    {
        var envelopeMock = new Mock<IInboundEnvelope<TMessage>>();
        envelopeMock.SetupGet(envelope => envelope.Message).Returns(message);
        envelopeMock
            .SetupGet(envelope => envelope.Endpoint)
            .Returns(new KafkaConsumerEndpoint(
                sourceTopic,
                Partition.Any,
                new KafkaConsumerEndpointConfiguration()));

        return envelopeMock.Object;
    }
}

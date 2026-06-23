using FlowChat.Core.Messaging;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging.Publishing;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka;

public sealed class SilverbackEventPublisherTests
{
    private static FlowChatSilverbackEventPublisher CreatePublisher(KafkaProducerSettingsRegistry? registry = null)
        => new(
            registry ?? new KafkaProducerSettingsRegistry(new Dictionary<Type, Core.Contracts.IKafkaProducerSettingsSection>()),
            Mock.Of<IPublisher>(),
            NullLogger<FlowChatSilverbackEventPublisher>.Instance);

    [Fact]
    public async Task PublishToOutboxAsync_WhenEnvelopeIsNull_ThrowsArgumentNullException()
    {
        var publisher = CreatePublisher();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync(
                (IntegrationEventEnvelope<TestIntegrationEvent>)null!,
                CancellationToken.None));
    }

    [Fact]
    public async Task PublishToOutboxAsync_WhenProducerOptionsAreMissing_ThrowsInvalidOperationException()
    {
        var publisher = CreatePublisher();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(
                new IntegrationEventEnvelope<TestIntegrationEvent>(
                    new TestIntegrationEvent(),
                    "user-1"),
                CancellationToken.None));

        exception.Message.Should().Contain("Kafka producer options");
    }

    private sealed record TestIntegrationEvent : IntegrationEvent;
}

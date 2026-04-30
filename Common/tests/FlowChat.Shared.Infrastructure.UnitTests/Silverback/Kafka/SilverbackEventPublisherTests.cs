using FlowChat.Core.Messaging;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging.Publishing;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka;

public sealed class SilverbackEventPublisherTests
{
    [Fact]
    public async Task PublishToOutboxAsync_WhenEnvelopeIsNull_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var publisher = new FlowChatSilverbackEventPublisher(
            services,
            Mock.Of<IPublisher>(),
            NullLogger<FlowChatSilverbackEventPublisher>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.Publish(
                (IntegrationEventEnvelope<TestIntegrationEvent>)null!,
                CancellationToken.None));
    }

    [Fact]
    public async Task PublishToOutboxAsync_WhenProducerOptionsAreMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var publisher = new FlowChatSilverbackEventPublisher(
            services,
            Mock.Of<IPublisher>(),
            NullLogger<FlowChatSilverbackEventPublisher>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.Publish(
                new IntegrationEventEnvelope<TestIntegrationEvent>(
                    new TestIntegrationEvent(),
                    "user-1"),
                CancellationToken.None));

        exception.Message.Should().Contain("Kafka producer options");
    }

    private sealed record TestIntegrationEvent : IntegrationEvent;
}

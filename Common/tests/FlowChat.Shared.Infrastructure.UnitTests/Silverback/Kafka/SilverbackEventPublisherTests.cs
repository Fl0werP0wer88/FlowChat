using FlowChat.Core.Contracts;
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
    public async Task PublishToOutboxAsync_WhenEventKeyIsMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var publisher = new FlowChatSilverbackEventPublisher(
            services,
            Mock.Of<IPublisher>(),
            NullLogger<FlowChatSilverbackEventPublisher>.Instance,
            Mock.Of<ISettingsProvider>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.Publish(
                new TestIntegrationEvent(),
                CancellationToken.None));

        exception.Message.Should().Contain("does not contain a Kafka key");
    }

    [Fact]
    public async Task PublishToOutboxAsync_WhenProducerOptionsAreMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var publisher = new FlowChatSilverbackEventPublisher(
            services,
            Mock.Of<IPublisher>(),
            NullLogger<FlowChatSilverbackEventPublisher>.Instance,
            Mock.Of<ISettingsProvider>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.Publish(
                new TestIntegrationEvent { Key = "user-1" },
                CancellationToken.None));

        exception.Message.Should().Contain("Kafka producer options");
    }

    private sealed class TestIntegrationEvent : IntegrationEvent;
}

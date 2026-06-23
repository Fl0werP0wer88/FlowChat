using System.Diagnostics;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Silverback.Behaviors;

public sealed class CustomSpanAttributesProducerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_WithEventTypeHeader_SetsActivityTagAndInvokesNext()
    {
        var headers = new MessageHeaderCollection(1);
        headers.Add(IntegrationMessageHeaders.EventType, "user-profile-created");

        var context = CreateProducerContext(headers);
        var behavior = new CustomSpanAttributesProducerBehavior();
        var nextInvoked = false;

        using var activity = new Activity("producer-test");
        activity.Start();

        await behavior.HandleAsync(
            context,
            (pipelineContext, cancellationToken) =>
            {
                nextInvoked = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextInvoked.Should().BeTrue();
        activity.GetTagItem(CustomSpanAttributesProducerBehavior.EventNameTag).Should().Be("user-profile-created");
        behavior.SortIndex.Should().Be(BrokerBehaviorsSortIndexes.Producer.MessageEnricher + 10);
    }

    [Fact]
    public async Task HandleAsync_WhenEventTypeHeaderIsMissing_DoesNotSetActivityTag()
    {
        var context = CreateProducerContext(new MessageHeaderCollection(0));
        var behavior = new CustomSpanAttributesProducerBehavior();

        using var activity = new Activity("missing-header-test");
        activity.Start();

        await behavior.HandleAsync(
            context,
            static (pipelineContext, cancellationToken) => ValueTask.CompletedTask,
            CancellationToken.None);

        activity.GetTagItem(CustomSpanAttributesProducerBehavior.EventNameTag).Should().BeNull();
    }

    private static ProducerPipelineContext CreateProducerContext(MessageHeaderCollection headers)
    {
        var envelopeMock = new Mock<IOutboundEnvelope>();
        envelopeMock.SetupGet(x => x.Headers).Returns(headers);

        var producerMock = new Mock<IProducer>();

        return new ProducerPipelineContext(
            envelopeMock.Object,
            producerMock.Object,
            [],
            static (pipelineContext, cancellationToken) => ValueTask.CompletedTask,
            new ServiceCollection().BuildServiceProvider());
    }
}

using System.Diagnostics;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Silverback.Behaviors;

public sealed class CustomSpanAttributesBehaviorTests
{
    [Fact]
    public async Task HandleAsync_ForProducerContext_WithEventTypeHeader_SetsActivityTagAndInvokesNext()
    {
        var headers = new MessageHeaderCollection(1);
        headers.Add(IntegrationMessageHeaders.EventType, "user-profile-created");

        var context = CreateProducerContext(headers);
        var behavior = new CustomSpanAttributesBehavior();
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
        activity.GetTagItem(CustomSpanAttributesBehavior.EventNameTag).Should().Be("user-profile-created");
        behavior.SortIndex.Should().Be(BrokerBehaviorsSortIndexes.Producer.MessageEnricher + 10);
    }

    [Fact]
    public async Task HandleAsync_ForConsumerContext_WithEventTypeHeader_SetsActivityTagAndInvokesNext()
    {
        var headers = new MessageHeaderCollection(1);
        headers.Add(IntegrationMessageHeaders.EventType, "chat-message-sent");

        var context = CreateConsumerContext(headers);
        var behavior = new CustomSpanAttributesBehavior();
        var nextInvoked = false;

        using var activity = new Activity("consumer-test");
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
        activity.GetTagItem(CustomSpanAttributesBehavior.EventNameTag).Should().Be("chat-message-sent");
    }

    [Fact]
    public async Task HandleAsync_WhenEventTypeHeaderIsMissing_DoesNotSetActivityTag()
    {
        var context = CreateProducerContext(new MessageHeaderCollection(0));
        var behavior = new CustomSpanAttributesBehavior();

        using var activity = new Activity("missing-header-test");
        activity.Start();

        await behavior.HandleAsync(
            context,
            static (pipelineContext, cancellationToken) => ValueTask.CompletedTask,
            CancellationToken.None);

        activity.GetTagItem(CustomSpanAttributesBehavior.EventNameTag).Should().BeNull();
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

    private static ConsumerPipelineContext CreateConsumerContext(MessageHeaderCollection headers)
    {
        var envelopeMock = new Mock<IRawInboundEnvelope>();
        envelopeMock.SetupGet(x => x.Headers).Returns(headers);

        var consumerMock = new Mock<IConsumer>();
        var sequenceStoreMock = new Mock<ISequenceStore>();

        return new ConsumerPipelineContext(
            envelopeMock.Object,
            consumerMock.Object,
            sequenceStoreMock.Object,
            [],
            new ServiceCollection().BuildServiceProvider());
    }
}

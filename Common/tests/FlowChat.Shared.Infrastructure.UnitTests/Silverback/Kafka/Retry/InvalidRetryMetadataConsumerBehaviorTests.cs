using FlowChat.Core.Exceptions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka.Retry;

public sealed class InvalidRetryMetadataConsumerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_InvalidMetadata_ThrowsAfterOffsetStoreSortIndex()
    {
        var headers = new MessageHeaderCollection();
        headers.Add("flowchat-invalid-retry-metadata", "invalid retry-at-utc");
        var context = CreateContext(headers);
        var behavior = new InvalidRetryMetadataConsumerBehavior();

        var action = async () => await behavior.HandleAsync(
            context,
            (_, _) => throw new InvalidOperationException("subscriber must not be called"),
            CancellationToken.None);

        await action.Should().ThrowAsync<NonTransientException>().WithMessage("invalid retry-at-utc");
        behavior.SortIndex.Should().Be(BrokerBehaviorsSortIndexes.Consumer.Publisher - 5);
    }

    [Fact]
    public async Task HandleAsync_ValidMetadata_InvokesNext()
    {
        var nextCalled = false;

        await new InvalidRetryMetadataConsumerBehavior().HandleAsync(
            CreateContext(new MessageHeaderCollection()),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    private static ConsumerPipelineContext CreateContext(MessageHeaderCollection headers)
    {
        var envelope = new Mock<IRawInboundEnvelope>();
        envelope.SetupGet(x => x.Headers).Returns(headers);
        return new ConsumerPipelineContext(
            envelope.Object,
            Mock.Of<IConsumer>(),
            Mock.Of<ISequenceStore>(),
            [],
            new ServiceCollection().BuildServiceProvider());
    }
}

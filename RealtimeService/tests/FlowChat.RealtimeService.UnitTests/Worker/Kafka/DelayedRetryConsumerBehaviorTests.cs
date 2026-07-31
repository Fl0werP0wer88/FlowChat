using FlowChat.Core.Exceptions;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Kafka.Retry;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace FlowChat.RealtimeService.UnitTests.Worker.Kafka;

public sealed class DelayedRetryConsumerBehaviorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
    private readonly ChatMessageV2ConsumerSettingsSection _settings = CreateSettings();
    private readonly Mock<IKafkaRetryPartitionController> _partitionController = new();

    [Fact]
    public async Task HandleAsync_DueRetry_InvokesNextImmediately()
    {
        var context = CreateContext(1, Now.AddSeconds(-1).ToString("O"));
        var nextCalled = false;

        await CreateBehavior().HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        _partitionController.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_FutureRetry_PausesAndResumesOnlySourcePartition()
    {
        var context = CreateContext(2, Now.AddMilliseconds(10).ToString("O"));
        _partitionController.Setup(x => x.IsAssigned(context.Consumer, It.IsAny<Confluent.Kafka.TopicPartition>()))
            .Returns(true);
        var nextCalled = false;

        await CreateBehavior().HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        _partitionController.Verify(
            x => x.Pause(context.Consumer, It.Is<Confluent.Kafka.TopicPartition>(p => p.Partition.Value == 2)),
            Times.Once);
        _partitionController.Verify(
            x => x.Resume(context.Consumer, It.Is<Confluent.Kafka.TopicPartition>(p => p.Partition.Value == 2)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PartitionRevokedDuringDelay_DoesNotResumePartition()
    {
        var context = CreateContext(1, Now.AddMilliseconds(10).ToString("O"));
        _partitionController.Setup(x => x.IsAssigned(context.Consumer, It.IsAny<Confluent.Kafka.TopicPartition>()))
            .Returns(false);

        await CreateBehavior().HandleAsync(
            context,
            (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);

        _partitionController.Verify(
            x => x.Resume(It.IsAny<IConsumer>(), It.IsAny<Confluent.Kafka.TopicPartition>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CanceledDelay_DoesNotResumeOrInvokeNext()
    {
        var context = CreateContext(0, Now.AddMinutes(1).ToString("O"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = async () => await CreateBehavior().HandleAsync(
            context,
            (_, _) => throw new InvalidOperationException("next must not be called"),
            cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        _partitionController.Verify(x => x.Pause(context.Consumer, It.IsAny<Confluent.Kafka.TopicPartition>()), Times.Once);
        _partitionController.Verify(
            x => x.Resume(It.IsAny<IConsumer>(), It.IsAny<Confluent.Kafka.TopicPartition>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_LegacyFirstTierWithoutRetryAt_InvokesNext()
    {
        var context = CreateContext(0, null);
        var nextCalled = false;

        await CreateBehavior().HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(0, "invalid")]
    public async Task HandleAsync_InvalidRetryAt_MarksEnvelopeAndContinues(int tierIndex, string? retryAt)
    {
        var context = CreateContext(tierIndex, retryAt);

        var nextCalled = false;

        await CreateBehavior().HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        context.Envelope.Headers.GetValue("flowchat-invalid-retry-metadata").Should().NotBeNullOrWhiteSpace();
    }

    private DelayedRetryConsumerBehavior CreateBehavior() =>
        new(
            new RealtimeRetryTopology([_settings]),
            new FixedTimeProvider(Now),
            _partitionController.Object,
            NullLogger<DelayedRetryConsumerBehavior>.Instance);

    private ConsumerPipelineContext CreateContext(int tierIndex, string? retryAt)
    {
        var headers = new MessageHeaderCollection();
        if (retryAt is not null)
            headers.Add(RetryMessageHeaders.RetryAtUtc, retryAt);

        var envelope = new Mock<IRawInboundEnvelope>();
        envelope.SetupGet(x => x.Headers).Returns(headers);
        envelope.SetupGet(x => x.BrokerMessageIdentifier)
            .Returns(new KafkaOffset(_settings.RetryTiers[tierIndex].Topic, tierIndex, 42));

        return new ConsumerPipelineContext(
            envelope.Object,
            Mock.Of<IConsumer>(),
            Mock.Of<ISequenceStore>(),
            [],
            new ServiceCollection().BuildServiceProvider());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static ChatMessageV2ConsumerSettingsSection CreateSettings() => new()
    {
        RetryTiers =
        [
            new RetryTierSettings { Topic = "retry-5s", Delay = TimeSpan.FromSeconds(5) },
            new RetryTierSettings { Topic = "retry-20s", Delay = TimeSpan.FromSeconds(20) },
            new RetryTierSettings { Topic = "retry-60s", Delay = TimeSpan.FromSeconds(60) },
            new RetryTierSettings { Topic = "retry-300s", Delay = TimeSpan.FromSeconds(300) }
        ]
    };
}

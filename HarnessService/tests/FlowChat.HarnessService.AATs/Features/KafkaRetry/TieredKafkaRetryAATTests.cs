using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.KafkaRetry;
using FlowChat.HarnessService.AATs.Infrastructure;
using FlowChat.HarnessService.Consumers.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;

namespace FlowChat.HarnessService.AATs.Features.KafkaRetry;

[Collection(HarnessAATCollectionFixture.CollectionName)]
[Trait("Category", "AAT")]
public sealed class TieredKafkaRetryAATTests : IAsyncLifetime
{
    private readonly HarnessAATCollectionFixture _fixture;
    private HarnessTieredRetryConsumerHost _consumerHost = null!;
    private HarnessOutboxPublisherHost? _outboxPublisherHost;
    private RetryPipelineTestPublisher _publisher = null!;

    public TieredKafkaRetryAATTests(HarnessAATCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _consumerHost = new HarnessTieredRetryConsumerHost(
            _fixture.ConnectionString,
            _fixture.BootstrapServers,
            _fixture.RetryPipeline);
        await _consumerHost.InitializeAsync();

        _publisher = new RetryPipelineTestPublisher(
            _fixture.BootstrapServers,
            _fixture.RetryPipeline.Topic);
        await _publisher.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _publisher.DisposeAsync();
        if (_outboxPublisherHost is not null)
            await _outboxPublisherHost.DisposeAsync();
        await _consumerHost.DisposeAsync();
    }

    [Fact]
    public async Task TransientFailure_FirstRetrySucceeds_ResultStoredOnce()
    {
        await StartOutboxPublisherAsync();
        var scenarioId = Guid.NewGuid();
        var published = await _publisher.PublishAsync(
            scenarioId,
            RetryPipelineTestFailureKind.Transient,
            failuresBeforeSuccess: 1);

        var result = await DbPoller.WaitForRetryResultAsync(
            _fixture.ConnectionString,
            scenarioId,
            TimeSpan.FromSeconds(30));
        var retryMessage = await KafkaMessagePoller.WaitForRetryMessageAsync(
            _fixture.BootstrapServers,
            _fixture.RetryPipeline.RetryTiers[0].Topic,
            scenarioId,
            TimeSpan.FromSeconds(10));

        result.AttemptCount.Should().Be(2);
        _consumerHost.GetAttemptCount(scenarioId).Should().Be(2);
        retryMessage.Should().NotBeNull();
        retryMessage!.KafkaKey.Should().Be(published.KafkaKey);
        retryMessage.Headers[IntegrationMessageHeaders.EventId].Should().Be(published.EventId.ToString("D"));
        retryMessage.Headers[RetryMessageHeaders.RetryAttempt].Should().Be("1");
        retryMessage.Headers[RetryMessageHeaders.OriginalTopic].Should().Be(_fixture.RetryPipeline.Topic);
        retryMessage.Headers.Should().ContainKey(RetryMessageHeaders.OriginalPartition);
        retryMessage.Headers.Should().ContainKey(RetryMessageHeaders.OriginalOffset);
    }

    [Fact]
    public async Task TransientFailure_AllRetriesFail_MessageEndsInDlq()
    {
        await StartOutboxPublisherAsync();
        var scenarioId = Guid.NewGuid();
        var published = await _publisher.PublishAsync(
            scenarioId,
            RetryPipelineTestFailureKind.Transient,
            failuresBeforeSuccess: int.MaxValue);

        var dlqMessage = await KafkaMessagePoller.WaitForRetryMessageAsync(
            _fixture.BootstrapServers,
            _fixture.RetryPipeline.DeadLetterTopic,
            scenarioId,
            TimeSpan.FromSeconds(30));

        dlqMessage.Should().NotBeNull();
        dlqMessage!.KafkaKey.Should().Be(published.KafkaKey);
        dlqMessage.Headers[IntegrationMessageHeaders.EventId].Should().Be(published.EventId.ToString("D"));
        dlqMessage.Headers[RetryMessageHeaders.RetryAttempt].Should().Be("4");
        dlqMessage.Headers[RetryMessageHeaders.OriginalTopic].Should().Be(_fixture.RetryPipeline.Topic);
        dlqMessage.Headers[RetryMessageHeaders.LastErrorType].Should().Be(typeof(TransientException).FullName);
        dlqMessage.Headers.Should().NotContainKey(RetryMessageHeaders.RetryAtUtc);
        _consumerHost.GetAttemptCount(scenarioId).Should().Be(5);
        (await DbPoller.RetryResultExistsAsync(_fixture.ConnectionString, scenarioId)).Should().BeFalse();

        foreach (var tier in _fixture.RetryPipeline.RetryTiers)
        {
            var retryMessage = await KafkaMessagePoller.WaitForRetryMessageAsync(
                _fixture.BootstrapServers,
                tier.Topic,
                scenarioId,
                TimeSpan.FromSeconds(5));
            retryMessage.Should().NotBeNull();
        }

        _consumerHost.LogCollector.Contains("An object of type").Should().BeFalse();
        _consumerHost.LogCollector.Contains("has already been added").Should().BeFalse();
    }

    [Fact]
    public async Task NonTransientFailure_OnMain_MessageMovesDirectlyToDlq()
    {
        await StartOutboxPublisherAsync();
        var scenarioId = Guid.NewGuid();
        var published = await _publisher.PublishAsync(
            scenarioId,
            RetryPipelineTestFailureKind.NonTransient,
            failuresBeforeSuccess: 1);

        var dlqMessage = await KafkaMessagePoller.WaitForRetryMessageAsync(
            _fixture.BootstrapServers,
            _fixture.RetryPipeline.DeadLetterTopic,
            scenarioId,
            TimeSpan.FromSeconds(30));

        dlqMessage.Should().NotBeNull();
        dlqMessage!.KafkaKey.Should().Be(published.KafkaKey);
        dlqMessage.Headers[IntegrationMessageHeaders.EventId].Should().Be(published.EventId.ToString("D"));
        dlqMessage.Headers[RetryMessageHeaders.OriginalTopic].Should().Be(_fixture.RetryPipeline.Topic);
        dlqMessage.Headers[RetryMessageHeaders.LastErrorType].Should().Be(typeof(NonTransientException).FullName);
        dlqMessage.Headers.Should().NotContainKey(RetryMessageHeaders.RetryAttempt);
        _consumerHost.GetAttemptCount(scenarioId).Should().Be(1);

        foreach (var tier in _fixture.RetryPipeline.RetryTiers)
        {
            var retryMessage = await KafkaMessagePoller.WaitForRetryMessageAsync(
                _fixture.BootstrapServers,
                tier.Topic,
                scenarioId,
                TimeSpan.FromMilliseconds(500));
            retryMessage.Should().BeNull();
        }
    }

    [Fact]
    public async Task PendingOutbox_AfterPublisherRestart_MessageIsNotLost()
    {
        var scenarioId = Guid.NewGuid();
        await using (var firstPublisherInstance = new PendingRetryOutboxWriter(
                         _fixture.ConnectionString,
                         _fixture.BootstrapServers,
                         _fixture.RetryPipeline.RetryTiers[0].Topic))
        {
            await firstPublisherInstance.InitializeAsync();
            await firstPublisherInstance.WriteAsync(
                scenarioId,
                _fixture.RetryPipeline.Topic);
        }

        await DbPoller.WaitForAtomicRetryTransferAsync(
            _fixture.ConnectionString,
            _fixture.RetryPipeline.Topic,
            TimeSpan.FromSeconds(30));

        _outboxPublisherHost = CreateOutboxPublisherHost();
        await _outboxPublisherHost.InitializeAsync();

        var result = await DbPoller.WaitForRetryResultAsync(
            _fixture.ConnectionString,
            scenarioId,
            TimeSpan.FromSeconds(30));

        result.AttemptCount.Should().Be(1);
        _consumerHost.GetAttemptCount(scenarioId).Should().Be(1);
    }

    private HarnessOutboxPublisherHost CreateOutboxPublisherHost() =>
        new(
            _fixture.ConnectionString,
            _fixture.BootstrapServers,
            _fixture.RetryPipeline.RetryTiers
                .Select(tier => tier.Topic)
                .Append(_fixture.RetryPipeline.DeadLetterTopic));

    private async Task StartOutboxPublisherAsync()
    {
        _outboxPublisherHost = CreateOutboxPublisherHost();
        await _outboxPublisherHost.InitializeAsync();
    }
}

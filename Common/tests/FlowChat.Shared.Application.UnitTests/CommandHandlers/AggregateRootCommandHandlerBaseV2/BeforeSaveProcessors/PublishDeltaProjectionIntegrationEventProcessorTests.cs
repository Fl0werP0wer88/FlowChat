using AutoMapper;
using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public sealed class PublishDeltaProjectionIntegrationEventProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenCollectionWasUpdated_PublishesAddedAndRemovedDeltas()
    {
        var aggregate = CreateAggregate();
        var before = new[]
        {
            new TestValue(Guid.NewGuid(), "Unchanged"),
            new TestValue(Guid.NewGuid(), "Removed")
        };
        var added = new TestValue(Guid.NewGuid(), "Added");
        var after = new[] { before[0], added };
        var mapperMock = CreateMapperMock(aggregate, before, after);
        var publishedEnvelopes = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>>();
        var publisherMock = CreatePublisherMock(publishedEnvelopes);
        var processor = new TestProcessor(mapperMock.Object, publisherMock.Object);

        processor.CaptureBeforeState(aggregate);
        await processor.ProcessAsync(new TestCommand(), aggregate, MutationType.Updated, CancellationToken.None);

        publishedEnvelopes.Should().HaveCount(2);
        publishedEnvelopes[0].Payload.Operation.Should().Be(DeltaOperationType.Added);
        publishedEnvelopes[0].Payload.Value.Should().BeEquivalentTo([added]);
        publishedEnvelopes[1].Payload.Operation.Should().Be(DeltaOperationType.Removed);
        publishedEnvelopes[1].Payload.Value.Should().BeEquivalentTo([before[1]]);
        publishedEnvelopes.Should().OnlyContain(envelope =>
            envelope.KafkaKey == aggregate.Id.Value.ToString("D")
            && envelope.Payload.SourceAggregateId == aggregate.Id.Value
            && envelope.Payload.SourceAggregateVersion == aggregate.Version);
    }

    [Fact]
    public async Task ProcessAsync_WhenOnlyValuesWithExistingKeysChanged_DoesNotPublish()
    {
        var aggregate = CreateAggregate();
        var id = Guid.NewGuid();
        var before = new[] { new TestValue(id, "Before") };
        var after = new[] { new TestValue(id, "After") };
        var mapperMock = CreateMapperMock(aggregate, before, after);
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = new TestProcessor(mapperMock.Object, publisherMock.Object);

        processor.CaptureBeforeState(aggregate);
        await processor.ProcessAsync(new TestCommand(), aggregate, MutationType.Updated, CancellationToken.None);

        publisherMock.Verify(
            publisher => publisher.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenAggregateWasCreated_PublishesAllValuesAsAdded()
    {
        var aggregate = CreateAggregate();
        var values = new[] { new TestValue(Guid.NewGuid(), "Added") };
        var mapperMock = CreateMapperMock(aggregate, values);
        var publishedEnvelopes = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>>();
        var publisherMock = CreatePublisherMock(publishedEnvelopes);
        var processor = new TestProcessor(mapperMock.Object, publisherMock.Object);

        await processor.ProcessAsync(new TestCommand(), aggregate, MutationType.Created, CancellationToken.None);

        publishedEnvelopes.Should().ContainSingle()
            .Which.Payload.Operation.Should().Be(DeltaOperationType.Added);
        publishedEnvelopes[0].Payload.Value.Should().BeEquivalentTo(values);
    }

    [Fact]
    public async Task ProcessAsync_WhenAggregateWasDeleted_UsesCapturedValuesForRemovedDelta()
    {
        var aggregate = CreateAggregate();
        var before = new[] { new TestValue(Guid.NewGuid(), "Removed") };
        var mapperMock = CreateMapperMock(aggregate, before, []);
        var publishedEnvelopes = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>>();
        var publisherMock = CreatePublisherMock(publishedEnvelopes);
        var processor = new TestProcessor(mapperMock.Object, publisherMock.Object);

        processor.CaptureBeforeState(aggregate);
        aggregate.Delete(UtcDateTimeOffset.UtcNow);
        await processor.ProcessAsync(new TestCommand(), aggregate, MutationType.Deleted, CancellationToken.None);

        publishedEnvelopes.Should().ContainSingle();
        publishedEnvelopes[0].Payload.Operation.Should().Be(DeltaOperationType.Removed);
        publishedEnvelopes[0].Payload.Value.Should().BeEquivalentTo(before);
        publishedEnvelopes[0].Payload.SourceAggregateDeletedAt.Should().Be(aggregate.DeletedAt!.Value);
    }

    [Fact]
    public async Task ProcessAsync_WhenUpdatedWithoutSnapshot_ThrowsInvalidOperationException()
    {
        var aggregate = CreateAggregate();
        var mapperMock = CreateMapperMock(aggregate, Array.Empty<TestValue>());
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = new TestProcessor(mapperMock.Object, publisherMock.Object);

        var act = () => processor.ProcessAsync(
            new TestCommand(),
            aggregate,
            MutationType.Updated,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A pre-mutation snapshot is required to process an updated delta projection.");
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationTypeIsUnchanged_ThrowsInvalidOperationException()
    {
        var aggregate = CreateAggregate();
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = new TestProcessor(mapperMock.Object, publisherMock.Object);

        var act = () => processor.ProcessAsync(
            new TestCommand(),
            aggregate,
            MutationType.Unchanged,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unchanged mutation type must not be processed as a delta projection operation.");
        mapperMock.Verify(
            mapper => mapper.Map<IEnumerable<TestValue>>(It.IsAny<TestAggregate>()),
            Times.Never);
    }

    private static TestAggregate CreateAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.SetCreated("system");
        aggregate.SetUpdated("system");
        aggregate.IncrementVersion();
        return aggregate;
    }

    private static Mock<IMapper> CreateMapperMock(
        TestAggregate aggregate,
        params IReadOnlyCollection<TestValue>[] mappedValues)
    {
        var mapperMock = new Mock<IMapper>();
        var callIndex = 0;
        mapperMock
            .Setup(mapper => mapper.Map<IEnumerable<TestValue>>(aggregate))
            .Returns(() => mappedValues[Math.Min(callIndex++, mappedValues.Length - 1)]);
        return mapperMock;
    }

    private static Mock<IOutboxIntegrationEventPublisher> CreatePublisherMock(
        ICollection<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>> publishedEnvelopes)
    {
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        publisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TestValue>>, CancellationToken>(
                (envelope, _) => publishedEnvelopes.Add(envelope))
            .Returns(Task.CompletedTask);
        return publisherMock;
    }

    private sealed record TestCommand;

    private sealed record TestValue(Guid Id, string Name);

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public TestAggregate(Guid id)
            : base(Id<TestAggregate>.FromGuid(id))
        {
        }
    }

    private sealed class TestProcessor(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher)
        : PublishDeltaProjectionIntegrationEventProcessor<TestCommand, TestAggregate, TestValue, Guid>(
            mapper,
            integrationEventPublisher)
    {
        protected override Guid GetKey(TestValue value) => value.Id;
    }
}

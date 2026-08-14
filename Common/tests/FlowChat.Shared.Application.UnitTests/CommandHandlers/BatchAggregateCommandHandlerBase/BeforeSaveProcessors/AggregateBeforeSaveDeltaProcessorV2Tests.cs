using AutoMapper;
using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public sealed class AggregateBeforeSaveDeltaProcessorV2Tests
{
    [Fact]
    public async Task ProcessAsync_WhenBatchIsValid_PublishesDeltaWithProjectionMetadata()
    {
        var aggregate = CreateAggregate();
        var readModel = new TestReadModel(aggregate.Id.Value, aggregate.Name);
        var projectionId = Guid.NewGuid();
        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<TestReadModel>(aggregate)).Returns(readModel);
        IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>? captured = null;
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        publisher.Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>, CancellationToken>(
                (envelope, _) => captured = envelope)
            .Returns(Task.CompletedTask);
        var processor = CreateProcessor(mapper, publisher);

        await processor.ProcessAsync(
            new TestCommand(),
            CreateBatch(aggregate, new DeltaProjectionMetadataV2(
                projectionId, 7)),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.KafkaKey.Should().Be(projectionId.ToString("D"));
        captured.Payload.ProjectionId.Should().Be(projectionId);
        captured.Payload.ProjectionRevision.Should().Be(7);
        captured.Payload.Delta.Should().ContainSingle();
        captured.Payload.Delta[0].SourceAggregateId.Should().Be(aggregate.Id.Value);
        captured.Payload.Delta[0].SourceAggregateVersion.Should().Be(aggregate.Version);
        captured.Payload.Delta[0].Operation.Should().Be(OperationType.Updated);
        captured.Payload.Delta[0].Value.Should().Be(readModel);
    }

    [Fact]
    public async Task ProcessAsync_WhenBatchIsEmpty_DoesNotRequireMetadataOrPublish()
    {
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);

        await processor.ProcessAsync(
            new TestCommand(),
            new AggregateDeltaBatch<TestAggregate>([]),
            CancellationToken.None);

        mapper.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisher);
    }

    [Fact]
    public async Task ProcessAsync_WhenMetadataIsMissing_ThrowsWithoutMappingOrPublishing()
    {
        var aggregate = CreateAggregate();
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);

        var action = () => processor.ProcessAsync(
            new TestCommand(),
            CreateBatch(aggregate, null),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Delta projection metadata is required for a non-empty delta.");
        mapper.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisher);
    }

    [Fact]
    public async Task ProcessAsync_WhenProjectionIdIsEmpty_ThrowsWithoutMappingOrPublishing()
    {
        var aggregate = CreateAggregate();
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);

        var action = () => processor.ProcessAsync(
            new TestCommand(),
            CreateBatch(aggregate, new DeltaProjectionMetadataV2(
                Guid.Empty, 1)),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Delta projection id cannot be empty.");
        mapper.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisher);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ProcessAsync_WhenProjectionRevisionIsInvalid_ThrowsWithoutMappingOrPublishing(int revision)
    {
        var aggregate = CreateAggregate();
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);

        var action = () => processor.ProcessAsync(
            new TestCommand(),
            CreateBatch(aggregate, new DeltaProjectionMetadataV2(
                Guid.NewGuid(), revision)),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Delta projection revision must be at least 1.");
        mapper.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisher);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationIsUnchanged_ThrowsBeforeMetadataValidation()
    {
        var aggregate = CreateAggregate();
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);
        var batch = new AggregateDeltaBatch<TestAggregate>(
            [new AggregateDeltaMutation<TestAggregate>(aggregate, MutationType.Unchanged)]);

        var action = () => processor.ProcessAsync(new TestCommand(), batch, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unchanged mutation type must not be processed as an aggregate delta projection operation.");
        VerifyNeverPublished(publisher);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationListIsNull_ThrowsWithoutPublishing()
    {
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);

        var action = () => processor.ProcessAsync(
            new TestCommand(),
            new AggregateDeltaBatch<TestAggregate>(null!),
            CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNeverPublished(publisher);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationContainsNullAggregate_ThrowsWithoutPublishing()
    {
        var mapper = new Mock<IMapper>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);
        var batch = new AggregateDeltaBatch<TestAggregate>(
            [new AggregateDeltaMutation<TestAggregate>(null!, MutationType.Updated)]);

        var action = () => processor.ProcessAsync(new TestCommand(), batch, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot contain a null aggregate*");
        VerifyNeverPublished(publisher);
    }

    [Fact]
    public async Task ProcessAsync_WhenMappingReturnsNull_ThrowsWithoutPublishing()
    {
        var aggregate = CreateAggregate();
        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<TestReadModel>(aggregate)).Returns((TestReadModel)null!);
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = CreateProcessor(mapper, publisher);

        var action = () => processor.ProcessAsync(
            new TestCommand(),
            CreateBatch(aggregate, new DeltaProjectionMetadataV2(
                Guid.NewGuid(), 1)),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mapping aggregate*returned null.");
        VerifyNeverPublished(publisher);
    }

    private static AggregateDeltaBatch<TestAggregate> CreateBatch(
        TestAggregate aggregate,
        DeltaProjectionMetadataV2? metadata) =>
        new(
            [new AggregateDeltaMutation<TestAggregate>(aggregate, MutationType.Updated)],
            metadata);

    private static AggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate, TestReadModel> CreateProcessor(
        Mock<IMapper> mapper,
        Mock<IOutboxIntegrationEventPublisher> publisher) =>
        new(mapper.Object, publisher.Object);

    private static TestAggregate CreateAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid(), "Name");
        aggregate.SetCreated("system");
        aggregate.SetUpdated("system");
        aggregate.IncrementVersion();
        return aggregate;
    }

    private static void VerifyNeverPublished(Mock<IOutboxIntegrationEventPublisher> publisher) =>
        publisher.Verify(x => x.PublishAsync(
            It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>>(),
            It.IsAny<CancellationToken>()), Times.Never);

    public sealed record TestCommand;
    public sealed record TestReadModel(Guid Id, string Name);

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public string Name { get; }

        public TestAggregate(Guid id, string name) : base(Id<TestAggregate>.FromGuid(id))
        {
            Name = name;
        }
    }
}

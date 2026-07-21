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
    public async Task ProcessAsync_WhenMutationsAreValid_PublishesSingleOrderedDeltaEvent()
    {
        var created = CreateAggregate("Created", MutationType.Created);
        var updated = CreateAggregate("Updated", MutationType.Updated);
        var deleted = CreateAggregate("Deleted", MutationType.Deleted);
        IReadOnlyList<AggregateDeltaMutation<TestAggregate>> mutations =
        [
            new(created, MutationType.Created),
            new(updated, MutationType.Updated),
            new(deleted, MutationType.Deleted)
        ];
        var command = new TestCommand(Guid.NewGuid());
        var expectedKafkaKey = Guid.NewGuid().ToString("D");
        var createdReadModel = new TestReadModel(created.Id.Value, created.Name);
        var updatedReadModel = new TestReadModel(updated.Id.Value, updated.Name);
        var deletedReadModel = new TestReadModel(deleted.Id.Value, deleted.Name);
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(x => x.Map<TestReadModel>(created)).Returns(createdReadModel);
        mapperMock.Setup(x => x.Map<TestReadModel>(updated)).Returns(updatedReadModel);
        mapperMock.Setup(x => x.Map<TestReadModel>(deleted)).Returns(deletedReadModel);
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        keyProviderMock
            .Setup(x => x.GetKafkaKey(command, mutations))
            .Returns(expectedKafkaKey);
        IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>? capturedEnvelope = null;
        CancellationToken capturedCancellationToken = default;
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        publisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>,
                CancellationToken>((envelope, token) =>
                {
                    capturedEnvelope = envelope;
                    capturedCancellationToken = token;
                })
            .Returns(Task.CompletedTask);
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);
        using var cancellationTokenSource = new CancellationTokenSource();

        await processor.ProcessAsync(command, mutations, cancellationTokenSource.Token);

        keyProviderMock.Verify(x => x.GetKafkaKey(command, mutations), Times.Once);
        publisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(expectedKafkaKey);
        capturedCancellationToken.Should().Be(cancellationTokenSource.Token);
        capturedEnvelope.Payload.Delta.Should().HaveCount(3);

        AssertDeltaItem(capturedEnvelope.Payload.Delta[0], created, createdReadModel, OperationType.Created);
        AssertDeltaItem(capturedEnvelope.Payload.Delta[1], updated, updatedReadModel, OperationType.Updated);
        AssertDeltaItem(capturedEnvelope.Payload.Delta[2], deleted, deletedReadModel, OperationType.Deleted);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationListIsEmpty_DoesNotResolveKeyMapOrPublish()
    {
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);

        await processor.ProcessAsync(new TestCommand(Guid.NewGuid()), [], CancellationToken.None);

        keyProviderMock.Verify(
            x => x.GetKafkaKey(
                It.IsAny<TestCommand>(),
                It.IsAny<IReadOnlyList<AggregateDeltaMutation<TestAggregate>>>()),
            Times.Never);
        mapperMock.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisherMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationTypeIsUnchanged_ThrowsWithoutResolvingKeyMappingOrPublishing()
    {
        var aggregate = CreateAggregate("Unchanged", MutationType.Updated);
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);

        var action = () => processor.ProcessAsync(
            new TestCommand(Guid.NewGuid()),
            [new AggregateDeltaMutation<TestAggregate>(aggregate, MutationType.Unchanged)],
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unchanged mutation type must not be processed as an aggregate delta projection operation.");
        VerifyNoWork(mapperMock, publisherMock, keyProviderMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationTypeIsUnsupported_ThrowsWithoutResolvingKeyMappingOrPublishing()
    {
        var aggregate = CreateAggregate("Unsupported", MutationType.Updated);
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);

        var action = () => processor.ProcessAsync(
            new TestCommand(Guid.NewGuid()),
            [new AggregateDeltaMutation<TestAggregate>(aggregate, (MutationType)int.MaxValue)],
            CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
        VerifyNoWork(mapperMock, publisherMock, keyProviderMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationListIsNull_ThrowsArgumentNullException()
    {
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);

        var action = () => processor.ProcessAsync(
            new TestCommand(Guid.NewGuid()),
            null!,
            CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNoWork(mapperMock, publisherMock, keyProviderMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationListContainsNullEntry_ThrowsWithoutResolvingKeyMappingOrPublishing()
    {
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);
        IReadOnlyList<AggregateDeltaMutation<TestAggregate>> mutations = [null!];

        var action = () => processor.ProcessAsync(
            new TestCommand(Guid.NewGuid()),
            mutations,
            CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot contain null entries*");
        VerifyNoWork(mapperMock, publisherMock, keyProviderMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenMutationContainsNullAggregate_ThrowsWithoutResolvingKeyMappingOrPublishing()
    {
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);
        IReadOnlyList<AggregateDeltaMutation<TestAggregate>> mutations =
            [new(null!, MutationType.Updated)];

        var action = () => processor.ProcessAsync(
            new TestCommand(Guid.NewGuid()),
            mutations,
            CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot contain a null aggregate*");
        VerifyNoWork(mapperMock, publisherMock, keyProviderMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenKeyProviderReturnsEmptyKey_ThrowsWithoutMappingOrPublishing()
    {
        var aggregate = CreateAggregate("Updated", MutationType.Updated);
        IReadOnlyList<AggregateDeltaMutation<TestAggregate>> mutations =
            [new(aggregate, MutationType.Updated)];
        var command = new TestCommand(Guid.NewGuid());
        var mapperMock = new Mock<IMapper>();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        keyProviderMock
            .Setup(x => x.GetKafkaKey(command, mutations))
            .Returns(string.Empty);
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);

        var action = () => processor.ProcessAsync(command, mutations, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Kafka key cannot be null or empty*");
        mapperMock.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisherMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenMappingReturnsNull_ThrowsWithoutPublishing()
    {
        var aggregate = CreateAggregate("Updated", MutationType.Updated);
        IReadOnlyList<AggregateDeltaMutation<TestAggregate>> mutations =
            [new(aggregate, MutationType.Updated)];
        var command = new TestCommand(Guid.NewGuid());
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(x => x.Map<TestReadModel>(aggregate))
            .Returns((TestReadModel)null!);
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var keyProviderMock = new Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>>();
        keyProviderMock
            .Setup(x => x.GetKafkaKey(command, mutations))
            .Returns(Guid.NewGuid().ToString("D"));
        var processor = CreateProcessor(mapperMock, publisherMock, keyProviderMock);

        var action = () => processor.ProcessAsync(command, mutations, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mapping aggregate*returned null.");
        VerifyNeverPublished(publisherMock);
    }

    private static AggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate, TestReadModel> CreateProcessor(
        Mock<IMapper> mapperMock,
        Mock<IOutboxIntegrationEventPublisher> publisherMock,
        Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>> keyProviderMock)
        => new(mapperMock.Object, publisherMock.Object, keyProviderMock.Object);

    private static TestAggregate CreateAggregate(string name, MutationType mutationType)
    {
        var aggregate = new TestAggregate(Guid.NewGuid(), name);
        aggregate.SetCreated("system");
        aggregate.SetUpdated("system");
        if (mutationType == MutationType.Deleted)
        {
            aggregate.Delete(UtcDateTimeOffset.UtcNow);
        }

        aggregate.IncrementVersion();
        return aggregate;
    }

    private static void AssertDeltaItem(
        DeltaProjectionItemV2<TestReadModel> item,
        TestAggregate aggregate,
        TestReadModel expectedValue,
        OperationType expectedOperation)
    {
        item.SourceAggregateId.Should().Be(aggregate.Id.Value);
        item.SourceAggregateCreatedAtUtc.Should().Be(aggregate.CreatedAtUtc.Value);
        item.SourceAggregateModifiedAtUtc.Should().Be(aggregate.LastModifiedAtUtc.Value);
        item.SourceAggregateDeletedAt.Should().Be(aggregate.DeletedAt?.Value);
        item.SourceAggregateVersion.Should().Be(aggregate.Version);
        item.Value.Should().Be(expectedValue);
        item.Operation.Should().Be(expectedOperation);
    }

    private static void VerifyNoWork(
        Mock<IMapper> mapperMock,
        Mock<IOutboxIntegrationEventPublisher> publisherMock,
        Mock<IAggregateDeltaProjectionKeyProviderV2<TestCommand, TestAggregate>> keyProviderMock)
    {
        keyProviderMock.Verify(
            x => x.GetKafkaKey(
                It.IsAny<TestCommand>(),
                It.IsAny<IReadOnlyList<AggregateDeltaMutation<TestAggregate>>>()),
            Times.Never);
        mapperMock.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        VerifyNeverPublished(publisherMock);
    }

    private static void VerifyNeverPublished(Mock<IOutboxIntegrationEventPublisher> publisherMock)
    {
        publisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TestReadModel>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public sealed record TestCommand(Guid Id);

    public sealed record TestReadModel(Guid Id, string Name);

    public sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public TestAggregate(Guid id, string name)
            : base(Id<TestAggregate>.FromGuid(id))
        {
            Name = name;
        }

        public string Name { get; }
    }
}

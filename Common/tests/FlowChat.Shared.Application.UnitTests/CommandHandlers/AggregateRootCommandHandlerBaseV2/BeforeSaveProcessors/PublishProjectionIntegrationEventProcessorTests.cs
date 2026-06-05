using AutoMapper;
using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public sealed class PublishProjectionIntegrationEventProcessorTests
{
    [Theory]
    [InlineData(AggregateState.Created, OperationType.Created)]
    [InlineData(AggregateState.Updated, OperationType.Updated)]
    [InlineData(AggregateState.Deleted, OperationType.Deleted)]
    public async Task ProcessAsync_WhenCalled_PublishesMappedProjectionIntegrationEvent(
        AggregateState aggregateState,
        OperationType expectedOperationType)
    {
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate(aggregateId, "Alpha");
        aggregate.IncrementVersion();
        var command = new TestCommand();
        var readModel = new TestReadModel(aggregateId, "Alpha");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IntegrationEventEnvelope<ProjectionIntegrationEvent<TestReadModel>>? capturedEnvelope = null;
        CancellationToken capturedCancellationToken = default;
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(x => x.Map<TestReadModel>(aggregate))
            .Returns(readModel);
        var integrationEventPublisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        integrationEventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ProjectionIntegrationEvent<TestReadModel>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                IntegrationEventEnvelope<ProjectionIntegrationEvent<TestReadModel>>,
                CancellationToken>((envelope, token) =>
                {
                    capturedEnvelope = envelope;
                    capturedCancellationToken = token;
                })
            .Returns(Task.CompletedTask);
        var processor = new PublishProjectionIntegrationEventProcessor<TestCommand, TestAggregate, TestReadModel>(
            mapperMock.Object,
            integrationEventPublisherMock.Object);

        await processor.ProcessAsync(command, aggregate, aggregateState, cancellationToken);

        mapperMock.Verify(x => x.Map<TestReadModel>(aggregate), Times.Once);
        integrationEventPublisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ProjectionIntegrationEvent<TestReadModel>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(aggregateId.ToString("D"));
        capturedEnvelope.Payload.SourceAggregateId.Should().Be(aggregateId);
        capturedEnvelope.Payload.Operation.Should().Be(expectedOperationType);
        capturedEnvelope.Payload.SourceAggregateVersion.Should().Be(aggregate.Version);
        capturedEnvelope.Payload.Value.Should().Be(readModel);
        capturedCancellationToken.Should().Be(cancellationToken);
    }

    [Fact]
    public async Task ProcessAsync_WhenAggregateStateIsUnchanged_ThrowsInvalidOperationException()
    {
        var aggregate = new TestAggregate(Guid.NewGuid(), "Alpha");
        var command = new TestCommand();
        var mapperMock = new Mock<IMapper>();
        var integrationEventPublisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var processor = new PublishProjectionIntegrationEventProcessor<TestCommand, TestAggregate, TestReadModel>(
            mapperMock.Object,
            integrationEventPublisherMock.Object);

        var act = () => processor.ProcessAsync(command, aggregate, AggregateState.Unchanged, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unchanged aggregate state must not be processed as a projection operation.");
        mapperMock.Verify(x => x.Map<TestReadModel>(It.IsAny<TestAggregate>()), Times.Never);
        integrationEventPublisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ProjectionIntegrationEvent<TestReadModel>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed record TestCommand;

    private sealed record TestReadModel(Guid Id, string Name);

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public TestAggregate(Guid id, string name)
            : base(Id<TestAggregate>.FromGuid(id))
        {
            Name = name;
        }

        public string Name { get; }
    }
}

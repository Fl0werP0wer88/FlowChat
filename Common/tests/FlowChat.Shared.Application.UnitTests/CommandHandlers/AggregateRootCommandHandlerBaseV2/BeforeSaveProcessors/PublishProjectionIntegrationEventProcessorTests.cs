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
    [InlineData(OperationType.Created)]
    [InlineData(OperationType.Updated)]
    [InlineData(OperationType.Deleted)]
    public async Task ProcessAsync_WhenCalled_PublishesMappedProjectionIntegrationEvent(OperationType operationType)
    {
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate(aggregateId, "Alpha");
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

        await processor.ProcessAsync(command, aggregate, operationType, cancellationToken);

        mapperMock.Verify(x => x.Map<TestReadModel>(aggregate), Times.Once);
        integrationEventPublisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ProjectionIntegrationEvent<TestReadModel>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(aggregateId.ToString("D"));
        capturedEnvelope.Payload.Operation.Should().Be(operationType);
        capturedEnvelope.Payload.Value.Should().Be(readModel);
        capturedCancellationToken.Should().Be(cancellationToken);
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

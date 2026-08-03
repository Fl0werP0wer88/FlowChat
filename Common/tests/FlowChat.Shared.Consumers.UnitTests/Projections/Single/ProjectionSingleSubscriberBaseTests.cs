using Confluent.Kafka;
using System.Diagnostics.CodeAnalysis;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.Projections.Single;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace FlowChat.Shared.Consumers.UnitTests.Projections.Single;

public sealed class ProjectionSingleSubscriberBaseTests
{
    [Fact]
    public async Task HandleAsync_ValidMessage_SendsSingleProjectionCommand()
    {
        ProjectionSingleCommand<TestProjectionValue>? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionSingleCommand<TestProjectionValue>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capturedCommand = (ProjectionSingleCommand<TestProjectionValue>)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
        var subscriber = CreateSubscriber(mediatorMock);
        var message = CreateMessage();

        await subscriber.HandleAsync(CreateEnvelope(message), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Item.Value.Id.Should().Be(message.Value.Id);
        capturedCommand.Item.Operation.Should().Be(message.Operation);
        capturedCommand.Item.SourceVersion.Should().Be(message.SourceAggregateVersion);
    }

    [Fact]
    public async Task HandleAsync_InvalidSourceVersion_ThrowsNonTransientException()
    {
        var subscriber = CreateSubscriber();

        var action = () => subscriber.HandleAsync(
            CreateEnvelope(CreateMessage(sourceVersion: 0)),
            CancellationToken.None);

        await action.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Payload does not contain valid SourceVersion.");
    }

    [Fact]
    public async Task HandleAsync_TransientFailure_ThrowsTransientException()
    {
        var subscriber = CreateFailingSubscriber(FailureKind.Transient);

        var action = () => subscriber.HandleAsync(CreateEnvelope(CreateMessage()), CancellationToken.None);

        await action.Should().ThrowAsync<TransientException>().WithMessage("projection failed");
    }

    [Fact]
    public async Task HandleAsync_IsolableFailure_ThrowsIsolableException()
    {
        var subscriber = CreateFailingSubscriber(FailureKind.Isolable);

        var action = () => subscriber.HandleAsync(CreateEnvelope(CreateMessage()), CancellationToken.None);

        await action.Should().ThrowAsync<IsolableException>().WithMessage("projection failed");
    }

    [Fact]
    public async Task HandleAsync_NonTransientFailure_ThrowsNonTransientException()
    {
        var subscriber = CreateFailingSubscriber(FailureKind.None);

        var action = () => subscriber.HandleAsync(CreateEnvelope(CreateMessage()), CancellationToken.None);

        await action.Should().ThrowAsync<NonTransientException>().WithMessage("projection failed");
    }

    [Fact]
    public async Task HandleAsync_FilteredMessage_CommitsConsumedOffsetWithoutSendingCommand()
    {
        var mediatorMock = new Mock<IMediator>();
        var offsetCommitterMock = new Mock<IConsumedOffsetCommitter>();
        offsetCommitterMock
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var subscriber = CreateSubscriber(
            mediatorMock,
            offsetCommitterMock,
            shouldMap: false);

        await subscriber.HandleAsync(CreateEnvelope(CreateMessage()), CancellationToken.None);

        offsetCommitterMock.Verify(
            x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        mediatorMock.Verify(
            x => x.Send(
                It.IsAny<ProjectionSingleCommand<TestProjectionValue>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_FilteredMessageWithFailedOffsetCommit_PropagatesException()
    {
        var offsetCommitterMock = new Mock<IConsumedOffsetCommitter>();
        offsetCommitterMock
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientException("offset commit failed"));
        var subscriber = CreateSubscriber(
            offsetCommitterMock: offsetCommitterMock,
            shouldMap: false);

        var action = () => subscriber.HandleAsync(CreateEnvelope(CreateMessage()), CancellationToken.None);

        await action.Should().ThrowAsync<TransientException>().WithMessage("offset commit failed");
    }

    private static TestProjectionSingleSubscriber CreateFailingSubscriber(
        FailureKind failureKind)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionSingleCommand<TestProjectionValue>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.UnExpected("projection failed", failureKind)));

        return CreateSubscriber(mediatorMock);
    }

    private static TestProjectionSingleSubscriber CreateSubscriber(
        Mock<IMediator>? mediatorMock = null,
        Mock<IConsumedOffsetCommitter>? offsetCommitterMock = null,
        bool shouldMap = true) =>
        new(
            (mediatorMock ?? new Mock<IMediator>()).Object,
            (offsetCommitterMock ?? new Mock<IConsumedOffsetCommitter>()).Object,
            shouldMap,
            NullLogger<TestProjectionSingleSubscriber>.Instance);

    private static ProjectionIntegrationEvent<TestReadModel> CreateMessage(int sourceVersion = 1) =>
        new()
        {
            SourceAggregateId = Guid.NewGuid(),
            SourceAggregateVersion = sourceVersion,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Value = new TestReadModel(Guid.NewGuid()),
            Operation = OperationType.Updated
        };

    private static IInboundEnvelope<ProjectionIntegrationEvent<TestReadModel>> CreateEnvelope(
        ProjectionIntegrationEvent<TestReadModel> message)
    {
        var envelopeMock = new Mock<IInboundEnvelope<ProjectionIntegrationEvent<TestReadModel>>>();
        envelopeMock.SetupGet(envelope => envelope.Message).Returns(message);
        envelopeMock.SetupGet(envelope => envelope.Headers).Returns(new MessageHeaderCollection(0));
        envelopeMock
            .SetupGet(envelope => envelope.Endpoint)
            .Returns(new KafkaConsumerEndpoint(
                "dev.flowchat.test-projection.v1",
                Partition.Any,
                new KafkaConsumerEndpointConfiguration()));

        return envelopeMock.Object;
    }

    private sealed class TestProjectionSingleSubscriber(
        IMediator mediator,
        IConsumedOffsetCommitter consumedOffsetCommitter,
        bool shouldMap,
        ILogger<TestProjectionSingleSubscriber> logger)
        : ProjectionSingleSubscriberBase<TestReadModel, TestProjectionValue>(
            mediator,
            consumedOffsetCommitter,
            logger)
    {
        protected override bool TryMapValue(
            ProjectionIntegrationEvent<TestReadModel> message,
            [NotNullWhen(true)]
            out TestProjectionValue? value)
        {
            value = shouldMap ? new TestProjectionValue(message.Value.Id) : null;
            return shouldMap;
        }
    }

    public sealed record TestReadModel(Guid Id);
    public sealed record TestProjectionValue(Guid Id);
}

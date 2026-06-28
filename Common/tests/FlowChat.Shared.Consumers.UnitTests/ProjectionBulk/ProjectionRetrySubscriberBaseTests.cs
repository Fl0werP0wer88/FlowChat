using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.Shared.Consumers.UnitTests.ProjectionBulk;

public sealed class ProjectionRetrySubscriberBaseTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandSucceeds_SendsSingleItemCommand()
    {
        ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capturedCommand = (ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
        var subscriber = CreateSubscriber(mediatorMock);
        var message = CreateMessage();

        await subscriber.HandleAsync(message, CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Which;
        item.Value.Payload.Should().Be(message.Value.Payload);
        item.Operation.Should().Be(message.Operation);
    }

    [Fact]
    public async Task HandleAsync_WhenMessageIsDelete_SendsItemWithDeletedOperationAndValue()
    {
        ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capturedCommand = (ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
        var subscriber = CreateSubscriber(mediatorMock);

        await subscriber.HandleAsync(CreateMessage(operation: OperationType.Deleted), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Which;
        item.Operation.Should().Be(OperationType.Deleted);
        item.Value.Payload.Should().Be("payload");
    }

    [Fact]
    public async Task HandleAsync_WhenSourceVersionIsInvalid_ThrowsNonTransientException()
    {
        var subscriber = CreateSubscriber();
        var message = CreateMessage(sourceVersion: 0);

        var act = () => subscriber.HandleAsync(message, CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Payload does not contain valid SourceVersion.");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithIsolableFailure_ThrowsIsolableException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", FailureKind.Isolable)));
        var subscriber = CreateSubscriber(mediatorMock);

        var act = () => subscriber.HandleAsync(CreateMessage(), CancellationToken.None);

        await act.Should().ThrowAsync<IsolableException>()
            .WithMessage("bulk failed");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithoutIsolableFailure_ThrowsNonTransientException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed")));
        var subscriber = CreateSubscriber(mediatorMock);

        var act = () => subscriber.HandleAsync(CreateMessage(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("bulk failed");
    }

    private static TestRetrySubscriber CreateSubscriber(Mock<IMediator>? mediatorMock = null)
    {
        mediatorMock ??= new Mock<IMediator>();

        return new TestRetrySubscriber(
            mediatorMock.Object,
            new TestProjectionValueFactory(),
            NullLogger.Instance);
    }

    private static ProjectionIntegrationEvent<TestReadModel> CreateMessage(
        int sourceVersion = 1,
        OperationType operation = OperationType.Updated) =>
        new()
        {
            SourceAggregateId = Guid.NewGuid(),
            SourceAggregateVersion = sourceVersion,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Value = new TestReadModel("payload"),
            Operation = operation
        };

    private sealed class TestRetrySubscriber(
        IMediator mediator,
        IProjectionValueFactory<TestReadModel, TestProjectionValue> valueFactory,
        ILogger logger)
        : ProjectionRetrySubscriberBase<TestReadModel, TestProjectionValue>(
            mediator,
            valueFactory,
            logger)
    {
        public Task HandleAsync(
            ProjectionIntegrationEvent<TestReadModel> message,
            CancellationToken cancellationToken) =>
            HandleRetryAsync(message, cancellationToken);
    }

    private sealed class TestProjectionValueFactory
        : IProjectionValueFactory<TestReadModel, TestProjectionValue>
    {
        public TestProjectionValue MapValue(ProjectionIntegrationEvent<TestReadModel> message) =>
            new(message.Value.Payload);
    }

    private sealed record TestProjectionValue(string Payload);

    private sealed record TestReadModel(string Payload);
}

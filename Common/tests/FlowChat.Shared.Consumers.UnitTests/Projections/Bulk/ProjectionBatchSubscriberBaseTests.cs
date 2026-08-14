using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.Projections.Bulk;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.Shared.Consumers.UnitTests.Projections.Bulk;

public sealed class ProjectionBatchSubscriberBaseTests
{
    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotSendCommand()
    {
        var mediatorMock = new Mock<IMediator>();
        var subscriber = CreateSubscriber(mediatorMock);

        await subscriber.HandleAsync(ToAsyncEnumerable([]), CancellationToken.None);

        mediatorMock.Verify(
            x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSourceVersionIsInvalid_ThrowsNonTransientException()
    {
        var subscriber = CreateSubscriber();
        var message = CreateMessage(sourceVersion: 0);

        var act = () => subscriber.HandleAsync(ToAsyncEnumerable([message]), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Payload does not contain valid SourceVersion.");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandSucceeds_SendsBulkCommand()
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
        var messages = new[] { CreateMessage(), CreateMessage() };

        await subscriber.HandleAsync(ToAsyncEnumerable(messages), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().HaveCount(2);
        capturedCommand.Items.Select(x => x.Value.Payload).Should().Equal(messages.Select(x => x.Value.Payload));
        capturedCommand.Items.Select(x => x.Operation).Should().Equal(messages.Select(x => x.Operation));
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsDuplicateKeys_SendsHighestVersionItem()
    {
        ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>? capturedCommand = null;
        var mediatorMock = CreateCapturingMediator(command => capturedCommand = command);
        var subscriber = CreateSubscriber(mediatorMock);
        var key = Guid.NewGuid();

        await subscriber.HandleAsync(
            ToAsyncEnumerable(
                [
                    CreateMessage(key, "older", sourceVersion: 1),
                    CreateMessage(key, "newer", sourceVersion: 3),
                    CreateMessage(key, "middle", sourceVersion: 2)
                ]),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Which;
        item.SourceVersion.Should().Be(3);
        item.Value.Payload.Should().Be("newer");
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsSameVersionDuplicateKeys_SendsLastItem()
    {
        ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>? capturedCommand = null;
        var mediatorMock = CreateCapturingMediator(command => capturedCommand = command);
        var subscriber = CreateSubscriber(mediatorMock);
        var key = Guid.NewGuid();

        await subscriber.HandleAsync(
            ToAsyncEnumerable(
                [
                    CreateMessage(key, "first", sourceVersion: 2),
                    CreateMessage(key, "last", sourceVersion: 2)
                ]),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Which;
        item.SourceVersion.Should().Be(2);
        item.Value.Payload.Should().Be("last");
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsDeduplicated_PreservesSelectedItemOrder()
    {
        ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>? capturedCommand = null;
        var mediatorMock = CreateCapturingMediator(command => capturedCommand = command);
        var subscriber = CreateSubscriber(mediatorMock);
        var firstKey = Guid.NewGuid();
        var secondKey = Guid.NewGuid();
        var thirdKey = Guid.NewGuid();

        await subscriber.HandleAsync(
            ToAsyncEnumerable(
                [
                    CreateMessage(firstKey, "first-stale", sourceVersion: 1),
                    CreateMessage(secondKey, "second", sourceVersion: 1),
                    CreateMessage(firstKey, "first-selected", sourceVersion: 2),
                    CreateMessage(thirdKey, "third", sourceVersion: 1)
                ]),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Select(x => x.Value.Payload)
            .Should()
            .Equal("second", "first-selected", "third");
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
        var message = CreateMessage(operation: OperationType.Deleted);

        await subscriber.HandleAsync(ToAsyncEnumerable([message]), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Which;
        item.Operation.Should().Be(OperationType.Deleted);
        item.Value.Payload.Should().Be(message.Value.Payload);
    }

    [Theory]
    [InlineData(FailureKind.None)]
    [InlineData(FailureKind.Transient)]
    [InlineData(FailureKind.Isolable)]
    public async Task HandleAsync_WhenCommandFails_ThrowsNonTransientException(FailureKind failureKind)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", failureKind)));
        var subscriber = CreateSubscriber(mediatorMock);

        var act = () => subscriber.HandleAsync(
            ToAsyncEnumerable([CreateMessage(), CreateMessage()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("bulk failed");
    }

    private static TestBatchSubscriber CreateSubscriber(Mock<IMediator>? mediatorMock = null)
    {
        mediatorMock ??= new Mock<IMediator>();

        return new TestBatchSubscriber(
            mediatorMock.Object,
            new TestProjectionValueFactory(),
            NullLogger.Instance);
    }

    private static Mock<IMediator> CreateCapturingMediator(
        Action<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>> capture)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capture((ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>)command))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        return mediatorMock;
    }

    private static ProjectionIntegrationEvent<TestReadModel> CreateMessage(
        int sourceVersion = 1,
        OperationType operation = OperationType.Updated) =>
        CreateMessage(Guid.NewGuid(), "payload", sourceVersion, operation);

    private static ProjectionIntegrationEvent<TestReadModel> CreateMessage(
        Guid key,
        string payload,
        int sourceVersion = 1,
        OperationType operation = OperationType.Updated) =>
        new()
        {
            SourceAggregateId = key,
            SourceAggregateVersion = sourceVersion,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Value = new TestReadModel(key, payload),
            Operation = operation
        };

    private static async IAsyncEnumerable<ProjectionIntegrationEvent<TestReadModel>> ToAsyncEnumerable(
        IEnumerable<ProjectionIntegrationEvent<TestReadModel>> messages)
    {
        foreach (var message in messages)
        {
            yield return message;
            await Task.Yield();
        }
    }

    private sealed class TestBatchSubscriber(
        IMediator mediator,
        IProjectionValueFactory<TestReadModel, TestProjectionValue, Guid> valueFactory,
        ILogger logger)
        : ProjectionBatchSubscriberBase<TestReadModel, TestProjectionValue, Guid>(
            mediator,
            valueFactory,
            logger)
    {
        public Task HandleAsync(
            IAsyncEnumerable<ProjectionIntegrationEvent<TestReadModel>> messages,
            CancellationToken cancellationToken) =>
            HandleBatchAsync(messages, cancellationToken);
    }

    private sealed class TestProjectionValueFactory
        : IProjectionValueFactory<TestReadModel, TestProjectionValue, Guid>
    {
        public TestProjectionValue MapValue(ProjectionIntegrationEvent<TestReadModel> message) =>
            new(message.Value.Id, message.Value.Payload);

        public Guid GetDeduplicationKey(TestProjectionValue value) =>
            value.Id;
    }

    private sealed record TestProjectionValue(Guid Id, string Payload);

    private sealed record TestReadModel(Guid Id, string Payload);
}

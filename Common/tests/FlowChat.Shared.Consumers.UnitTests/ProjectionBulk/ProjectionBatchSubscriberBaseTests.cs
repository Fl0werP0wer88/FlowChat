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
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.Shared.Consumers.UnitTests.ProjectionBulk;

public sealed class ProjectionBatchSubscriberBaseTests
{
    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotSendCommand()
    {
        var mediatorMock = new Mock<IMediator>();
        var publisherMock = new Mock<IPublisher>();
        var subscriber = CreateSubscriber(mediatorMock, publisherMock);

        await subscriber.HandleAsync(ToAsyncEnumerable([]), CancellationToken.None);

        mediatorMock.Verify(
            x => x.Send(It.IsAny<ProjectionBulkCommand<TestProjectionItem>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        publisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
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
        ProjectionBulkCommand<TestProjectionItem>? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<ProjectionBulkCommand<TestProjectionItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capturedCommand = (ProjectionBulkCommand<TestProjectionItem>)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
        var subscriber = CreateSubscriber(mediatorMock);
        var messages = new[] { CreateMessage(), CreateMessage() };

        await subscriber.HandleAsync(ToAsyncEnumerable(messages), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().HaveCount(2);
        capturedCommand.Items.Select(x => x.Id).Should().Equal(messages.Select(x => x.SourceAggregateId));
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithIsolableFailure_PublishesOriginalMessagesToRetry()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<ProjectionBulkCommand<TestProjectionItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", FailureKind.Isolable)));
        var publisherMock = new Mock<IPublisher>();
        publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var subscriber = CreateSubscriber(mediatorMock, publisherMock);
        var messages = new[] { CreateMessage(), CreateMessage() };

        await subscriber.HandleAsync(ToAsyncEnumerable(messages), CancellationToken.None);

        foreach (var message in messages)
        {
            publisherMock.Verify(
                x => x.PublishAsync(message, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithoutIsolableFailure_ThrowsNonTransientException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<ProjectionBulkCommand<TestProjectionItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed")));
        var subscriber = CreateSubscriber(mediatorMock);

        var act = () => subscriber.HandleAsync(ToAsyncEnumerable([CreateMessage()]), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("bulk failed");
    }

    private static TestBatchSubscriber CreateSubscriber(
        Mock<IMediator>? mediatorMock = null,
        Mock<IPublisher>? publisherMock = null)
    {
        mediatorMock ??= new Mock<IMediator>();
        publisherMock ??= new Mock<IPublisher>();

        return new TestBatchSubscriber(
            mediatorMock.Object,
            publisherMock.Object,
            new TestProjectionCommandItemFactory(),
            NullLogger.Instance);
    }

    private static ProjectionIntegrationEvent<TestReadModel> CreateMessage(int sourceVersion = 1) =>
        new()
        {
            SourceAggregateId = Guid.NewGuid(),
            SourceAggregateVersion = sourceVersion,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Value = new TestReadModel("payload")
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
        IPublisher publisher,
        IProjectionCommandItemFactory<TestReadModel, TestProjectionItem> itemFactory,
        ILogger logger)
        : ProjectionBatchSubscriberBase<TestReadModel, TestProjectionItem>(
            mediator,
            publisher,
            itemFactory,
            logger)
    {
        public Task HandleAsync(
            IAsyncEnumerable<ProjectionIntegrationEvent<TestReadModel>> messages,
            CancellationToken cancellationToken) =>
            HandleBatchAsync(messages, cancellationToken);
    }

    private sealed class TestProjectionCommandItemFactory
        : IProjectionCommandItemFactory<TestReadModel, TestProjectionItem>
    {
        public TestProjectionItem MapItem(ProjectionIntegrationEvent<TestReadModel> message) =>
            new(message.SourceAggregateId, message.Value.Payload);
    }

    private sealed record TestProjectionItem(Guid Id, string Payload);

    private sealed record TestReadModel(string Payload);
}

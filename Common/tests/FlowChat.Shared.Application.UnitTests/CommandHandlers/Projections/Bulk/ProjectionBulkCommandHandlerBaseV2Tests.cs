using FluentAssertions;
using FlowChat.Core.Results;
using MediatR;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.Projections.Bulk;

public sealed class ProjectionBulkCommandHandlerBaseV2Tests
{
    [Fact]
    public async Task Handle_WhenCommandHasItems_BulkUpsertsItemsAndCommitsOffsetInTransaction()
    {
        var command = new ProjectionBulkCommand<TestProjectionItem>([new TestProjectionItem(Guid.NewGuid())]);
        var executionOrder = new List<string>();
        var unitOfWorkMock = CreateUnitOfWorkMock(executionOrder);
        var repositoryMock = new Mock<IProjectionBulkRepository<TestProjectionItem>>();

        repositoryMock
            .Setup(x => x.BulkUpsertOrSoftDeleteAsync(command.Items, It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("bulk"))
            .Returns(Task.CompletedTask);

        unitOfWorkMock
            .As<IConsumedOffsetCommitter>()
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("offset"))
            .Returns(Task.CompletedTask);

        var handler = new ProjectionBulkCommandHandlerBaseV2<
            ProjectionBulkCommand<TestProjectionItem>,
            TestProjectionItem,
            IProjectionBulkRepository<TestProjectionItem>>(
            unitOfWorkMock.Object,
            repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        executionOrder.Should().Equal("transaction-start", "bulk", "offset", "transaction-end");
        repositoryMock.Verify(
            x => x.BulkUpsertOrSoftDeleteAsync(command.Items, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWorkMock
            .As<IConsumedOffsetCommitter>()
            .Verify(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommandHasNoItems_CommitsOffsetAndSkipsBulkUpsert()
    {
        var command = new ProjectionBulkCommand<TestProjectionItem>([]);
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var repositoryMock = new Mock<IProjectionBulkRepository<TestProjectionItem>>();

        unitOfWorkMock
            .As<IConsumedOffsetCommitter>()
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ProjectionBulkCommandHandlerBaseV2<
            ProjectionBulkCommand<TestProjectionItem>,
            TestProjectionItem,
            IProjectionBulkRepository<TestProjectionItem>>(
            unitOfWorkMock.Object,
            repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repositoryMock.Verify(
            x => x.BulkUpsertOrSoftDeleteAsync(It.IsAny<IReadOnlyCollection<TestProjectionItem>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWorkMock
            .As<IConsumedOffsetCommitter>()
            .Verify(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(List<string>? executionOrder = null)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                async (operation, cancellationToken) =>
                {
                    executionOrder?.Add("transaction-start");
                    var result = await operation(cancellationToken);
                    executionOrder?.Add("transaction-end");
                    return result;
                });

        return unitOfWorkMock;
    }

    public sealed record TestProjectionItem(Guid Id);
}

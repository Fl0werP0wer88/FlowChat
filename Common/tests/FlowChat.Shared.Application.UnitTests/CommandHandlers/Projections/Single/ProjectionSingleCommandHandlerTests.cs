using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.Projections.Single;

public sealed class ProjectionSingleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_UpsertsProjectionAndCommitsOffsetInTransaction()
    {
        var item = new ProjectionCommandItem<TestProjectionValue>(
            new TestProjectionValue(Guid.NewGuid()),
            OperationType.Updated,
            1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);
        var command = new ProjectionSingleCommand<TestProjectionValue>(item);
        var executionOrder = new List<string>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var repositoryMock = new Mock<IProjectionSingleRepository<TestProjectionValue>>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                async (operation, cancellationToken) =>
                {
                    executionOrder.Add("transaction-start");
                    var result = await operation(cancellationToken);
                    executionOrder.Add("transaction-end");
                    return result;
                });
        repositoryMock
            .Setup(x => x.UpsertOrSoftDeleteAsync(item, It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("projection"))
            .Returns(Task.CompletedTask);
        unitOfWorkMock
            .As<IConsumedOffsetCommitter>()
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("offset"))
            .Returns(Task.CompletedTask);
        var handler = new ProjectionSingleCommandHandler<TestProjectionValue>(
            unitOfWorkMock.Object,
            repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        executionOrder.Should().Equal("transaction-start", "projection", "offset", "transaction-end");
        repositoryMock.Verify(
            x => x.UpsertOrSoftDeleteAsync(item, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public sealed record TestProjectionValue(Guid Id);
}

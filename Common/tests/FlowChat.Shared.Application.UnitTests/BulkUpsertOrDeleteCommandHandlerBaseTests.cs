using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class BulkUpsertOrDeleteCommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenAllItemsAreUpserts_CallsUpsertOnly()
    {
        var items = new[]
        {
            new TestCommandItem(Id<TestValue>.New(), new TestValue(Guid.NewGuid(), "Alpha")),
            new TestCommandItem(Id<TestValue>.New(), new TestValue(Guid.NewGuid(), "Beta"))
        };
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<int>.Success(items.Length));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpsertedCount.Should().Be(items.Length);
        result.Value.DeletedCount.Should().Be(0);
        executorMock.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()), Times.Once);
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAllItemsAreDeletes_CallsDeleteOnly()
    {
        var items = new[]
        {
            new TestCommandItem(Id<TestValue>.New(), null),
            new TestCommandItem(Id<TestValue>.New(), null)
        };
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<int>.Success(items.Length));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpsertedCount.Should().Be(0);
        result.Value.DeletedCount.Should().Be(items.Length);
        executorMock.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()), Times.Never);
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMixedItems_CallsBothExecutorsWithCorrectPartitions()
    {
        var upsertId = Id<TestValue>.New();
        var deleteId = Id<TestValue>.New();
        var upsertValue = new TestValue(Guid.NewGuid(), "Alpha");
        var items = new[]
        {
            new TestCommandItem(upsertId, upsertValue),
            new TestCommandItem(deleteId, null)
        };
        IReadOnlyCollection<TestValue>? capturedUpserts = null;
        IReadOnlyCollection<Id<TestValue>>? capturedDeleteIds = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TestValue>, CancellationToken>((i, _) => capturedUpserts = i)
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Id<TestValue>>, CancellationToken>((i, _) => capturedDeleteIds = i)
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedCount.Should().Be(2);
        result.Value.UpsertedCount.Should().Be(1);
        result.Value.DeletedCount.Should().Be(1);
        capturedUpserts.Should().ContainSingle().Which.Should().Be(upsertValue);
        capturedDeleteIds.Should().ContainSingle().Which.Should().Be(deleteId);
    }

    [Fact]
    public async Task Handle_WhenNoItems_ReturnsEmptyResultWithoutCallingExecutor()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand([]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(BulkUpsertOrDeleteCommandResult.Empty);
        executorMock.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()), Times.Never);
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUpsertFails_ReturnsFailureWithoutCallingDelete()
    {
        var items = new[]
        {
            new TestCommandItem(Id<TestValue>.New(), new TestValue(Guid.NewGuid(), "Alpha")),
            new TestCommandItem(Id<TestValue>.New(), null)
        };
        var failure = FlowChatResult<int>.Failure(DomainError.Conflict("upsert failed"));
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("upsert failed");
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDeleteFails_ReturnsFailure()
    {
        var items = new[] { new TestCommandItem(Id<TestValue>.New(), null) };
        var failure = FlowChatResult<int>.Failure(DomainError.Conflict("delete failed"));
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<Id<TestValue>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("delete failed");
    }

    [Fact]
    public async Task Handle_ExecutesInsideTransaction()
    {
        var items = new[] { new TestCommandItem(Id<TestValue>.New(), new TestValue(Guid.NewGuid(), "A")) };
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestValue>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        await handler.Handle(new TestCommand(items), CancellationToken.None);

        unitOfWorkMock.Verify(
            x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<BulkUpsertOrDeleteCommandResult>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var mock = new Mock<IUnitOfWork>();
        mock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<BulkUpsertOrDeleteCommandResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<BulkUpsertOrDeleteCommandResult>>>,
                CancellationToken>((operation, ct) => operation(ct));
        return mock;
    }

    public sealed record TestValue(Guid Id, string Name);

    public sealed record TestCommandItem(Id<TestValue> EntityId, TestValue? Value)
        : IBulkCommandItem<TestValue>;

    public sealed record TestCommand(IReadOnlyCollection<TestCommandItem> Items)
        : IBulkUpsertOrDeleteCommand<TestCommandItem, TestValue>;

    private sealed class TestHandler : BulkUpsertOrDeleteCommandHandlerBase<TestCommand, TestCommandItem, TestValue>
    {
        public TestHandler(IUnitOfWork unitOfWork, IBulkExecutor<TestValue> bulkExecutor)
            : base(unitOfWork, bulkExecutor)
        {
        }
    }
}

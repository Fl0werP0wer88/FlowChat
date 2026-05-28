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
        var items = new[] { new TestItem(Guid.NewGuid(), false), new TestItem(Guid.NewGuid(), false) };
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<int>.Success(items.Length));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpsertedCount.Should().Be(items.Length);
        result.Value.DeletedCount.Should().Be(0);
        executorMock.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAllItemsAreDeletes_CallsDeleteOnly()
    {
        var items = new[] { new TestItem(Guid.NewGuid(), true), new TestItem(Guid.NewGuid(), true) };
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<int>.Success(items.Length));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpsertedCount.Should().Be(0);
        result.Value.DeletedCount.Should().Be(items.Length);
        executorMock.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMixedItems_CallsBothExecutorsWithCorrectPartitions()
    {
        var upsertItem = new TestItem(Guid.NewGuid(), false);
        var deleteItem = new TestItem(Guid.NewGuid(), true);
        var items = new[] { upsertItem, deleteItem };
        IReadOnlyCollection<TestItem>? capturedUpserts = null;
        IReadOnlyCollection<TestItem>? capturedDeletes = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TestItem>, CancellationToken>((i, _) => capturedUpserts = i)
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TestItem>, CancellationToken>((i, _) => capturedDeletes = i)
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedCount.Should().Be(2);
        result.Value.UpsertedCount.Should().Be(1);
        result.Value.DeletedCount.Should().Be(1);
        capturedUpserts.Should().ContainSingle().Which.Should().Be(upsertItem);
        capturedDeletes.Should().ContainSingle().Which.Should().Be(deleteItem);
    }

    [Fact]
    public async Task Handle_WhenNoItems_ReturnsEmptyResultWithoutCallingExecutor()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand([]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(BulkUpsertOrDeleteCommandResult.Empty);
        executorMock.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUpsertFails_ReturnsFailureWithoutCallingDelete()
    {
        var items = new[] { new TestItem(Guid.NewGuid(), false), new TestItem(Guid.NewGuid(), true) };
        var failure = FlowChatResult<int>.Failure(DomainError.Conflict("upsert failed"));
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("upsert failed");
        executorMock.Verify(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDeleteFails_ReturnsFailure()
    {
        var items = new[] { new TestItem(Guid.NewGuid(), true) };
        var failure = FlowChatResult<int>.Failure(DomainError.Conflict("delete failed"));
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);
        var handler = new TestHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestCommand(items), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("delete failed");
    }

    [Fact]
    public async Task Handle_WhenItemsNeedMapping_MapsItemsBeforeRouting()
    {
        var commandItems = new[]
        {
            new TestCommandItem(Guid.NewGuid(), "alpha", false),
            new TestCommandItem(Guid.NewGuid(), "beta", true)
        };
        IReadOnlyCollection<TestMappedItem>? capturedUpserts = null;
        IReadOnlyCollection<TestMappedItem>? capturedDeletes = null;
        var unitOfWorkMock = CreateMappedUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestMappedItem>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestMappedItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TestMappedItem>, CancellationToken>((i, _) => capturedUpserts = i)
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        executorMock
            .Setup(x => x.DeleteAsync(It.IsAny<IReadOnlyCollection<TestMappedItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TestMappedItem>, CancellationToken>((i, _) => capturedDeletes = i)
            .ReturnsAsync(FlowChatResult<int>.Success(1));
        var handler = new TestMappedHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(new TestMappedCommand(commandItems), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedUpserts.Should().ContainSingle().Which.Name.Should().Be("ALPHA");
        capturedDeletes.Should().ContainSingle().Which.Name.Should().Be("BETA");
    }

    [Fact]
    public async Task Handle_ExecutesInsideTransaction()
    {
        var items = new[] { new TestItem(Guid.NewGuid(), false) };
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkExecutor<TestItem>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestItem>>(), It.IsAny<CancellationToken>()))
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

    private static Mock<IUnitOfWork> CreateMappedUnitOfWorkMock()
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

    public sealed record TestCommand(IReadOnlyCollection<TestItem> Items) : IBulkUpsertOrDeleteCommand<TestItem>;

    public sealed record TestItem(Guid Id, bool MarkedForDeletion) : IBulkCommandItem;

    public sealed record TestMappedCommand(IReadOnlyCollection<TestCommandItem> Items)
        : IBulkUpsertOrDeleteCommand<TestCommandItem>;

    public sealed record TestCommandItem(Guid Id, string Name, bool MarkedForDeletion) : IBulkCommandItem;

    public sealed record TestMappedItem(Guid Id, string Name);

    private sealed class TestHandler : BulkUpsertOrDeleteCommandHandlerBase<TestCommand, TestItem>
    {
        public TestHandler(IUnitOfWork unitOfWork, IBulkExecutor<TestItem> bulkExecutor)
            : base(unitOfWork, bulkExecutor)
        {
        }
    }

    private sealed class TestMappedHandler
        : BulkUpsertOrDeleteCommandHandlerBase<TestMappedCommand, TestCommandItem, TestMappedItem>
    {
        public TestMappedHandler(IUnitOfWork unitOfWork, IBulkExecutor<TestMappedItem> bulkExecutor)
            : base(unitOfWork, bulkExecutor)
        {
        }

        protected override TestMappedItem MapItem(TestCommandItem item)
            => new(item.Id, item.Name.ToUpperInvariant());
    }
}

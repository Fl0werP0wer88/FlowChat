using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class BulkUpsertCommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenItemsExist_DelegatesBulkUpsertInsideTransaction()
    {
        var items = new[] { new TestBulkItem(Guid.NewGuid()), new TestBulkItem(Guid.NewGuid()) };
        var expected = BulkUpsertCommandResult.FromRequestedCount(items.Length);
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var bulkUpsertExecutorMock = new Mock<IBulkUpsertExecutor<TestBulkItem>>();
        bulkUpsertExecutorMock
            .Setup(x => x.UpsertAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(expected));
        var handler = new TestBulkUpsertCommandHandler(unitOfWorkMock.Object, bulkUpsertExecutorMock.Object);

        var result = await handler.Handle(new TestBulkUpsertCommand(items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
        unitOfWorkMock.Verify(
            x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<BulkUpsertCommandResult>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        bulkUpsertExecutorMock.Verify(
            x => x.UpsertAsync(items, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoItems_ReturnsEmptyResultWithoutCallingBulkUpsert()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var bulkUpsertExecutorMock = new Mock<IBulkUpsertExecutor<TestBulkItem>>();
        var handler = new TestBulkUpsertCommandHandler(unitOfWorkMock.Object, bulkUpsertExecutorMock.Object);

        var result = await handler.Handle(new TestBulkUpsertCommand([]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(BulkUpsertCommandResult.Empty);
        bulkUpsertExecutorMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<IReadOnlyCollection<TestBulkItem>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBulkUpsertFails_ReturnsFailureResult()
    {
        var items = new[] { new TestBulkItem(Guid.NewGuid()) };
        var failure = FlowChatResult<BulkUpsertCommandResult>.Failure(DomainError.Conflict("bulk conflict"));
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var bulkUpsertExecutorMock = new Mock<IBulkUpsertExecutor<TestBulkItem>>();
        bulkUpsertExecutorMock
            .Setup(x => x.UpsertAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);
        var handler = new TestBulkUpsertCommandHandler(unitOfWorkMock.Object, bulkUpsertExecutorMock.Object);

        var result = await handler.Handle(new TestBulkUpsertCommand(items), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("bulk conflict");
    }

    [Fact]
    public async Task Handle_WhenCommandItemsNeedMapping_MapsItemsBeforeBulkUpsert()
    {
        var commandItems = new[]
        {
            new TestBulkCommandItem(Guid.NewGuid(), "Alpha"),
            new TestBulkCommandItem(Guid.NewGuid(), "Beta")
        };
        IReadOnlyCollection<TestBulkMappedItem>? capturedItems = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var bulkUpsertExecutorMock = new Mock<IBulkUpsertExecutor<TestBulkMappedItem>>();
        bulkUpsertExecutorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<TestBulkMappedItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TestBulkMappedItem>, CancellationToken>((items, _) => capturedItems = items)
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(
                BulkUpsertCommandResult.FromRequestedCount(commandItems.Length)));
        var handler = new TestMappedBulkUpsertCommandHandler(unitOfWorkMock.Object, bulkUpsertExecutorMock.Object);

        var result = await handler.Handle(new TestMappedBulkUpsertCommand(commandItems), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedItems.Should().NotBeNull();
        capturedItems!.Select(x => x.Name).Should().BeEquivalentTo(["ALPHA", "BETA"]);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<BulkUpsertCommandResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<BulkUpsertCommandResult>>>,
                CancellationToken>((operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock;
    }

    public sealed record TestBulkUpsertCommand(IReadOnlyCollection<TestBulkItem> Items)
        : IBulkUpsertCommand<TestBulkItem>;

    public sealed record TestBulkItem(Guid Id);

    public sealed record TestMappedBulkUpsertCommand(IReadOnlyCollection<TestBulkCommandItem> Items)
        : IBulkUpsertCommand<TestBulkCommandItem>;

    public sealed record TestBulkCommandItem(Guid Id, string Name);

    public sealed record TestBulkMappedItem(Guid Id, string Name);

    private sealed class TestBulkUpsertCommandHandler
        : BulkUpsertCommandHandlerBase<TestBulkUpsertCommand, TestBulkItem>
    {
        public TestBulkUpsertCommandHandler(
            IUnitOfWork unitOfWork,
            IBulkUpsertExecutor<TestBulkItem> bulkUpsertExecutor)
            : base(unitOfWork, bulkUpsertExecutor)
        {
        }
    }

    private sealed class TestMappedBulkUpsertCommandHandler
        : BulkUpsertCommandHandlerBase<TestMappedBulkUpsertCommand, TestBulkCommandItem, TestBulkMappedItem>
    {
        public TestMappedBulkUpsertCommandHandler(
            IUnitOfWork unitOfWork,
            IBulkUpsertExecutor<TestBulkMappedItem> bulkUpsertExecutor)
            : base(unitOfWork, bulkUpsertExecutor)
        {
        }

        protected override TestBulkMappedItem MapItem(TestBulkCommandItem item)
        {
            return new TestBulkMappedItem(item.Id, item.Name.ToUpperInvariant());
        }
    }
}

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
}

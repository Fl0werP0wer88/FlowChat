using FlowChat.Shared.Infrastructure.Redis;
using FluentAssertions;
using Moq;
using StackExchange.Redis;

namespace FlowChat.Shared.Infrastructure.UnitTests.Redis;

public sealed class RedisUnitOfWorkTests
{
    private readonly Mock<IConnectionMultiplexer> _multiplexerMock = new();
    private readonly Mock<IDatabase> _databaseMock = new();
    private readonly Mock<ITransaction> _transactionMock = new();

    public RedisUnitOfWorkTests()
    {
        _multiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(_databaseMock.Object);

        _databaseMock
            .Setup(x => x.CreateTransaction(It.IsAny<object?>()))
            .Returns(_transactionMock.Object);
    }

    [Fact]
    public void Constructor_WhenConnectionMultiplexerIsNull_ThrowsArgumentNullException()
    {
        var act = () => new RedisUnitOfWork(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionMultiplexer");
    }

    [Fact]
    public async Task SaveChangesAsync_Always_ReturnsZero()
    {
        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        var result = await unitOfWork.SaveChangesAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_ExecutesTransactionAndReturnsResult()
    {
        _transactionMock
            .Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        var result = await unitOfWork.ExecuteInTransactionAsync(
            _ => Task.FromResult(42),
            CancellationToken.None);

        result.Should().Be(42);
        _transactionMock.Verify(x => x.ExecuteAsync(It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTransactionNotCommitted_ThrowsInvalidOperationException()
    {
        // EXEC returns nil when a WATCH condition detects a concurrent modification
        _transactionMock
            .Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        var act = () => unitOfWork.ExecuteInTransactionAsync(
            _ => Task.FromResult(0),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EXEC returned nil*");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_PropagatesExceptionWithoutExecutingTransaction()
    {
        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        var act = () => unitOfWork.ExecuteInTransactionAsync<int>(
            _ => throw new InvalidOperationException("operation failed"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("operation failed");

        _transactionMock.Verify(x => x.ExecuteAsync(It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationIsNull_ThrowsArgumentNullException()
    {
        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        var act = () => unitOfWork.ExecuteInTransactionAsync<int>(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => unitOfWork.ExecuteInTransactionAsync(
            _ => Task.FromResult(0),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void GetActiveDatabase_WhenOutsideTransaction_ReturnsPlainDatabase()
    {
        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        var db = unitOfWork.GetActiveDatabase();

        db.Should().BeSameAs(_databaseMock.Object);
    }

    [Fact]
    public async Task GetActiveDatabase_WhenInsideTransaction_ReturnsActiveTransaction()
    {
        _transactionMock
            .Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        IDatabaseAsync? capturedDatabase = null;

        await unitOfWork.ExecuteInTransactionAsync(
            _ =>
            {
                capturedDatabase = unitOfWork.GetActiveDatabase();
                return Task.FromResult(0);
            },
            CancellationToken.None);

        capturedDatabase.Should().BeSameAs(_transactionMock.Object);
    }

    [Fact]
    public async Task GetActiveDatabase_AfterTransactionCompletes_ReturnsFallbackDatabase()
    {
        _transactionMock
            .Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        await unitOfWork.ExecuteInTransactionAsync(
            _ => Task.FromResult(0),
            CancellationToken.None);

        // Transaction context must be cleared after ExecuteAsync so the UoW is safe to reuse
        var db = unitOfWork.GetActiveDatabase();
        db.Should().BeSameAs(_databaseMock.Object);
    }

    [Fact]
    public async Task GetActiveDatabase_AfterOperationThrows_ReturnsFallbackDatabase()
    {
        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);

        try
        {
            await unitOfWork.ExecuteInTransactionAsync<int>(
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None);
        }
        catch (InvalidOperationException) { }

        // Transaction context must be cleared even when the operation throws
        var db = unitOfWork.GetActiveDatabase();
        db.Should().BeSameAs(_databaseMock.Object);
    }

    [Fact]
    public async Task GetActiveDatabase_WhenConcurrentTransactionsRun_KeepsTransactionContextPerAsyncFlow()
    {
        var firstTransactionMock = new Mock<ITransaction>();
        var secondTransactionMock = new Mock<ITransaction>();
        var createdTransactions = new Queue<ITransaction>([firstTransactionMock.Object, secondTransactionMock.Object]);

        _databaseMock
            .Setup(x => x.CreateTransaction(It.IsAny<object?>()))
            .Returns(() => createdTransactions.Dequeue());

        firstTransactionMock
            .Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        secondTransactionMock
            .Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var unitOfWork = new RedisUnitOfWork(_multiplexerMock.Object);
        var firstOperationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowFirstCapture = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCaptured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCaptured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTransactions = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IDatabaseAsync? firstCapturedDatabase = null;
        IDatabaseAsync? secondCapturedDatabase = null;

        var firstTask = unitOfWork.ExecuteInTransactionAsync(async _ =>
        {
            firstOperationStarted.SetResult();
            await allowFirstCapture.Task;
            firstCapturedDatabase = unitOfWork.GetActiveDatabase();
            firstCaptured.SetResult();
            await releaseTransactions.Task;

            return 1;
        }, CancellationToken.None);

        await firstOperationStarted.Task;

        var secondTask = unitOfWork.ExecuteInTransactionAsync(async _ =>
        {
            secondCapturedDatabase = unitOfWork.GetActiveDatabase();
            secondCaptured.SetResult();
            await releaseTransactions.Task;

            return 2;
        }, CancellationToken.None);

        await secondCaptured.Task;
        allowFirstCapture.SetResult();
        await firstCaptured.Task;
        releaseTransactions.SetResult();

        await Task.WhenAll(firstTask, secondTask);

        firstCapturedDatabase.Should().BeSameAs(firstTransactionMock.Object);
        secondCapturedDatabase.Should().BeSameAs(secondTransactionMock.Object);
        unitOfWork.GetActiveDatabase().Should().BeSameAs(_databaseMock.Object);
    }
}

using FlowChat.Shared.Application;
using StackExchange.Redis;

namespace FlowChat.Shared.Infrastructure.Redis;

public class RedisUnitOfWork(IConnectionMultiplexer connectionMultiplexer)
    : IUnitOfWork, IRedisTransactionContext
{
    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));

    private ITransaction? _currentTransaction;

    /// <inheritdoc />
    /// <remarks>
    /// Returns the active MULTI/EXEC transaction when called inside ExecuteInTransactionAsync,
    /// so that Redis repositories queue their commands atomically. Outside a transaction,
    /// returns the plain IDatabase (fire-and-forget semantics).
    /// </remarks>
    public IDatabaseAsync GetActiveDatabase() =>
        (IDatabaseAsync?)_currentTransaction ?? _connectionMultiplexer.GetDatabase();

    // Redis operations are immediately persisted — there are no pending changes to flush.
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        _currentTransaction = database.CreateTransaction();

        try
        {
            var result = await operation(cancellationToken);

            // Commands queued by repositories via GetActiveDatabase() are sent atomically here.
            var committed = await _currentTransaction.ExecuteAsync();
            if (!committed)
            {
                // EXEC returns nil when a WATCH condition fails (optimistic concurrency guard).
                throw new InvalidOperationException(
                    "Redis transaction was not committed — EXEC returned nil. " +
                    "A WATCH condition may have detected a concurrent modification.");
            }

            return result;
        }
        finally
        {
            _currentTransaction = null;
        }
    }

    public void Dispose() { }
}

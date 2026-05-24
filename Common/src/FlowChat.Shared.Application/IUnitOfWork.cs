namespace FlowChat.Shared.Application;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, CancellationToken, Task<T>> beforeCommitOperation,
        Func<Exception, CancellationToken, Task> beforeRollbackHook,
        CancellationToken cancellationToken);
}


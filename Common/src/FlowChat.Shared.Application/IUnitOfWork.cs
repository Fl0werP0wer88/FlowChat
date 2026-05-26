using FlowChat.Core.Results;

namespace FlowChat.Shared.Application;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    Task<FlowChatResult<T>> ExecuteCommandInTransactionAsync<T>(
        Func<CancellationToken, Task<FlowChatResult<T>>> operation,
        CancellationToken cancellationToken)
        where T : notnull;
}


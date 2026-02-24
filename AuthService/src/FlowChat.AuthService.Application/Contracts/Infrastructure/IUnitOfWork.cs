namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

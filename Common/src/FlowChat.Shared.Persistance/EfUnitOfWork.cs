using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlowChat.Shared.Persistance;

public class EfUnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork
    where TDbContext : DbContext
{
    protected readonly TDbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await DbContext.SaveChangesAsync(cancellationToken);

    public virtual async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> beforeSaveOperation,
        CancellationToken cancellationToken) =>
        await ExecuteInTransactionAsync(
            beforeSaveOperation,
            static (result, _) => Task.FromResult(result),
            cancellationToken);

    public virtual async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> beforeSaveOperation,
        Func<T, CancellationToken, Task<T>> beforeCommitOperation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(beforeSaveOperation);
        ArgumentNullException.ThrowIfNull(beforeCommitOperation);
        cancellationToken.ThrowIfCancellationRequested();

        var strategy = DbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            EnrichTransaction(transaction);

            try
            {
                var result = await beforeSaveOperation(cancellationToken);
                await DbContext.SaveChangesAsync(cancellationToken);
                result = await beforeCommitOperation(result, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                OnFinally();
            }
        });
    }

    protected virtual void EnrichTransaction(IDbContextTransaction transaction) { }

    protected virtual void OnFinally() { }

    public void Dispose() => DbContext.Dispose();
}

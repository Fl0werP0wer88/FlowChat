using FlowChat.Core.Results;
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
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var strategy = DbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            EnrichTransaction(transaction);

            try
            {
                var result = await operation(cancellationToken);
                await DbContext.SaveChangesAsync(cancellationToken);
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

    public virtual async Task<FlowChatResult<T>> ExecuteCommandInTransactionAsync<T>(
        Func<CancellationToken, Task<FlowChatResult<T>>> operation,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var strategy = DbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            EnrichTransaction(transaction);

            try
            {
                var result = await operation(cancellationToken);

                if (result.IsSuccess)
                {
                    await DbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

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

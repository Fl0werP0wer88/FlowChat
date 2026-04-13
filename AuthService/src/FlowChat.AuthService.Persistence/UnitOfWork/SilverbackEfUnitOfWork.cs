using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback;
using Silverback.Storage;

namespace FlowChat.AuthService.Persistence.UnitOfWork;

public sealed class SilverbackEfUnitOfWork(AppDbContext dbContext, ISilverbackContext silverbackContext)
    : EfUnitOfWork<AppDbContext>(dbContext)
{
    public override async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var strategy = DbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            // Enlist Silverback in the same DB transaction so outbox messages and business data
            // are committed atomically. ownTransaction: false means EF owns commit/rollback,
            // not Silverback — prevents double-commit on success or swallowed rollbacks on failure.
            silverbackContext.EnlistDbTransaction(transaction.GetDbTransaction(), ownTransaction: false);

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
                // Always clean up the Silverback storage transaction reference even on exception,
                // so the context is not left in a broken state for the next operation.
                silverbackContext.ClearStorageTransaction();
            }
        });
    }
}

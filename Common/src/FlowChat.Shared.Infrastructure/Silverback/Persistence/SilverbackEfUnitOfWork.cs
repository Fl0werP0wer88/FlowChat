using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback;
using Silverback.Storage;

namespace FlowChat.Shared.Infrastructure.Silverback.Persistence;

public sealed class SilverbackEfUnitOfWork<TDbContext>(
    TDbContext dbContext,
    ISilverbackContext silverbackContext)
    : EfUnitOfWork<TDbContext>(dbContext)
    where TDbContext : DbContext
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

            // Silverback must share the EF transaction so business data and the outbox commit atomically
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
                // Clear the storage transaction even after failures so the scoped context is reusable
                silverbackContext.ClearStorageTransaction();
            }
        });
    }
}

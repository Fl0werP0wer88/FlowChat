using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback;
using Silverback.Storage;

namespace FlowChat.AuthService.Persistence.UnitOfWork;

public sealed class AppDbContextUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly ISilverbackContext _silverbackContext;

    public AppDbContextUnitOfWork(AppDbContext dbContext, ISilverbackContext silverbackContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _silverbackContext = silverbackContext ?? throw new ArgumentNullException(nameof(silverbackContext));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _silverbackContext.EnlistDbTransaction(transaction.GetDbTransaction(), ownTransaction: false);

            try
            {
                var result = await operation(cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
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
                _silverbackContext.ClearStorageTransaction();
            }
        });
    }

    public void Dispose() => _dbContext.Dispose();
}


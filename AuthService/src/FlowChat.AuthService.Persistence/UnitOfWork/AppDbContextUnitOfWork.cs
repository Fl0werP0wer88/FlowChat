using FlowChat.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace FlowChat.AuthService.Persistence.UnitOfWork;

public sealed class AppDbContextUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly IDbContextOutbox<AppDbContext> _outbox;

    public AppDbContextUnitOfWork(
        AppDbContext dbContext,
        IDbContextOutbox<AppDbContext> outbox)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }
 
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = await _dbContext.SaveChangesAsync(cancellationToken);
        await _outbox.FlushOutgoingMessagesAsync();
        return changes;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                await _outbox.FlushOutgoingMessagesAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose() => _dbContext.Dispose();
}

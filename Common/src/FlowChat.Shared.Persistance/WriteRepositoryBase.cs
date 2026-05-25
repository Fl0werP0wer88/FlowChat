using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance;

public class WriteRepositoryBase<TAggregate, TEntity>(DbContext dbContext)
    : IWriteRepository<TAggregate, TEntity>
    where TEntity : class, IEntity<TEntity>, IAggregateRoot
    where TAggregate : class, TEntity
{
    protected readonly DbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public virtual async Task<TAggregate?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TAggregate>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual async Task<TAggregate> AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TAggregate>().AddAsync(aggregate, cancellationToken);
        return aggregate;
    }

    public virtual Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TAggregate>().Update(aggregate);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TAggregate>().Remove(aggregate);
        return Task.CompletedTask;
    }
}

public class WriteRepositoryBase<TEntity>(DbContext dbContext)
    : WriteRepositoryBase<TEntity, TEntity>(dbContext), IWriteRepository<TEntity>
    where TEntity : class, IEntity<TEntity>, IAggregateRoot;

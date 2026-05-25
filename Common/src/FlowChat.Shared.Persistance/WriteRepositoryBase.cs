using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance;

public class WriteRepositoryBase<TEntity>(DbContext dbContext) : IWriteRepository<TEntity>
    where TEntity : class, IEntity<TEntity>, IAggregateRoot
{
    protected readonly DbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public virtual async Task<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }
}

public class WriteRepositoryBase<TAggregate, TEntity>(DbContext dbContext)
    : WriteRepositoryBase<TEntity>(dbContext), IWriteRepository<TAggregate, TEntity>
    where TEntity : class, IEntity<TEntity>, IAggregateRoot
    where TAggregate : class, TEntity
{
    public virtual new async Task<TAggregate?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default)
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


using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance;

public class WriteRepositoryBase<TAggregate, TEntity>(DbContext dbContext)
    : IWriteRepository<TAggregate, TEntity>
    where TEntity : class, IEntity<TEntity>
    where TAggregate : class, TEntity, IAggregateRoot
{
    protected readonly DbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));


    public virtual async Task<TAggregate?> GetByIdAsync(Id<TAggregate> id, CancellationToken cancellationToken = default)
    {
        var entityId = Id<TEntity>.FromId(id);
        return await DbContext.Set<TAggregate>()
            .FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);
    }

    public virtual async Task<TAggregate?> GetByIdAsync(
    CancellationToken cancellationToken = default,
    params object?[] keyValues)
    {
        var entityType = DbContext.Model.FindEntityType(typeof(TAggregate))
            ?? throw new InvalidOperationException(
                $"Entity type {typeof(TAggregate).Name} is not mapped.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type {typeof(TAggregate).Name} has no primary key.");

        if (primaryKey.Properties.Count != keyValues.Length)
        {
            throw new ArgumentException(
                $"{typeof(TAggregate).Name} expects {primaryKey.Properties.Count} key values, " +
                $"but received {keyValues.Length}.",
                nameof(keyValues));
        }

        return await DbContext.Set<TAggregate>()
            .FindAsync(keyValues, cancellationToken);
    }

    public virtual async Task<TAggregate> AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TAggregate>().AddAsync(aggregate, cancellationToken);
        return aggregate;
    }

    public virtual Task SoftDeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        aggregate.Delete(UtcDateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}

public class WriteRepositoryBase<TAggregate>(DbContext dbContext)
    : WriteRepositoryBase<TAggregate, TAggregate>(dbContext), IWriteRepository<TAggregate>
    where TAggregate : class, IEntity<TAggregate>, IAggregateRoot;

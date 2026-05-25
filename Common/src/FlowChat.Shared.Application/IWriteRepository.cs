using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IWriteRepository<TEntity> where TEntity : class, IEntity<TEntity>, IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}

public interface IWriteRepository<TAggregate, TEntity>
    where TEntity : class, IEntity<TEntity>
    where TAggregate : class, TEntity, IAggregateRoot
{
    Task<TAggregate?> GetByIdAsync(Id<TAggregate> id, CancellationToken cancellationToken = default);
    Task<TAggregate> AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}


using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IWriteRepository<TEntity> where TEntity : class, IEntity<TEntity>, IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}


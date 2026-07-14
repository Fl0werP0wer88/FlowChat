using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IWriteRepository<TAggregate> : IWriteRepository<TAggregate, TAggregate> where TAggregate : class, IEntity<TAggregate>, IAggregateRoot
{
}

public interface IWriteRepository<TAggregate, TEntity>
    where TEntity : class, IEntity<TEntity>
    where TAggregate : class, TEntity, IAggregateRoot
{
    Task<TAggregate?> GetByIdAsync(Id<TAggregate> id, CancellationToken cancellationToken = default);
    Task<TAggregate?> GetByIdAsync(CancellationToken cancellationToken = default, params IId?[] keyValues);
    Task<TAggregate> AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}

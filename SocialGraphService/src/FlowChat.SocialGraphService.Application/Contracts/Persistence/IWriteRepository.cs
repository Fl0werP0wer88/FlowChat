using FlowChat.Domain.Abstractions;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IWriteRepository<T> where T : class, IEntity<T>, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

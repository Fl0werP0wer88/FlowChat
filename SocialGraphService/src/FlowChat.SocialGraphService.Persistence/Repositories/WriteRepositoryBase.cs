using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public class WriteRepositoryBase<T>(AppDbContext dbContext) : IWriteRepository<T> where T : class, IEntity<T>, IAggregateRoot
{
    protected readonly AppDbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>()
            .FirstOrDefaultAsync(x => x.Id.Value == id, cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<T>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }
}

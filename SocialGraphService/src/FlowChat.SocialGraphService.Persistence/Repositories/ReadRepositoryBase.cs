using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public abstract class ReadRepositoryBase<TEntity, TDto>(AppDbContext dbContext) : IReadRepository<TDto>
    where TEntity : class, IEntity<TEntity>
    where TDto : class
{
    protected readonly AppDbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    protected virtual IQueryable<TEntity> Query => DbContext.Set<TEntity>().AsNoTracking();
    protected abstract Expression<Func<TEntity, TDto>> MapToDto { get; }

    public virtual async Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Id.Value == id)
            .Select(MapToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Select(MapToDto)
            .ToListAsync(cancellationToken);
    }
}

using FlowChat.Core.Contracts;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.Shared.Persistance;

public abstract class ReadRepositoryBase<TEntity, TDto>(DbContext dbContext) : IReadRepository<TDto>
    where TEntity : class, IEntity<TEntity>
    where TDto : class, IDbReadResponse
{
    protected readonly DbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    protected virtual IQueryable<TEntity> Query => DbContext.Set<TEntity>().AsNoTracking();
    protected abstract Expression<Func<TEntity, TDto>> MapToDto { get; }

    public virtual async Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var typedId = Id<TEntity>.FromGuid(id);
        return await Query
            .Where(x => x.Id == typedId)
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


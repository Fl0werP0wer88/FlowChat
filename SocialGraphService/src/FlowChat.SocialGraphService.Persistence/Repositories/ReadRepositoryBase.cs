using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public class ReadRepositoryBase<T>(AppDbContext dbContext) : IReadRepository<T> where T : class
{
    protected readonly AppDbContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>()
            .FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

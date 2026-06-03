using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance;

public abstract class ReadRepositoryBase
{
    protected static IQueryable<TEntity> Active<TEntity>(IQueryable<TEntity> query)
        where TEntity : ReadEntityBase
    {
        return query
            .AsNoTracking()
            .Where(entity => entity.DeletedAt == null);
    }
}

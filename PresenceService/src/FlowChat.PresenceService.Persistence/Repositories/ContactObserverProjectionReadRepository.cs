using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class ContactObserverProjectionReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase, IContactObserverProjectionReadRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyCollection<Guid>> GetObserverUserIdsAsync(
        Guid observedUserId,
        CancellationToken cancellationToken = default)
    {
        return await Active(_dbContext.ContactObserverProjections)
            .Where(x => x.ObservedUserId == observedUserId)
            .Select(x => x.ObserverUserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetObservedUserIdsAsync(
        Guid observerUserId,
        CancellationToken cancellationToken = default)
    {
        return await Active(_dbContext.ContactObserverProjections)
            .Where(x => x.ObserverUserId == observerUserId)
            .Select(x => x.ObservedUserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}

using FlowChat.PresenceService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class ContactObserverProjectionReadRepository(AppDbContext dbContext)
    : IContactObserverProjectionReadRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyCollection<Guid>> GetObserverUserIdsAsync(
        Guid observedUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ContactObserverProjections
            .Where(x => x.ObservedUserId == observedUserId)
            .Select(x => x.ObserverUserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}

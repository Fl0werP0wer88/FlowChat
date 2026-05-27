using FlowChat.PresenceService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class ContactObserverProjectionWriteRepository(AppDbContext dbContext)
    : IContactObserverProjectionWriteRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<bool> DeleteAsync(
        Guid observedUserId,
        Guid observerUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ContactObserverProjections.FirstOrDefaultAsync(
            x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId,
            cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.ContactObserverProjections.Remove(entity);
        return true;
    }
}

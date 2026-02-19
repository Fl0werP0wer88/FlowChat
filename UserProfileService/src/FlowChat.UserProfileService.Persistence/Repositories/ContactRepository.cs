using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public class ContactRepository : RepositoryBase<Contact>, IContactRepository
{
    public ContactRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .AsNoTracking()
            .Include(x => x.Requester)
            .Include(x => x.Addressee)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        ContactStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Contacts
            .AsNoTracking()
            .Include(x => x.Requester)
            .Include(x => x.Addressee)
            .Where(x => x.RequesterId == userId || x.AddresseeId == userId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetIncomingPendingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .AsNoTracking()
            .Include(x => x.Requester)
            .Include(x => x.Addressee)
            .Where(x => x.AddresseeId == userId && x.Status == ContactStatus.Pending)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RelationshipExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .AnyAsync(
                x => (x.RequesterId == userAId && x.AddresseeId == userBId)
                     || (x.RequesterId == userBId && x.AddresseeId == userAId),
                cancellationToken);
    }

    public override async Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .Include(x => x.Requester)
            .Include(x => x.Addressee)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

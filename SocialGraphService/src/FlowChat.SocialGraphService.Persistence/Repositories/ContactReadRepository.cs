using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase<Contact>(dbContext), IContactReadRepository
{
    public async Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id.Value == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Contacts
            .AsNoTracking()
            .Where(x => x.OwnerUserId == userId);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RelationshipExistsAsync(
        Guid ownerUserId,
        Guid contactUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .AnyAsync(
                x => x.OwnerUserId == ownerUserId && x.ContactUserId == contactUserId,
                cancellationToken);
    }

    public override async Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .FirstOrDefaultAsync(x => x.Id.Value == id, cancellationToken);
    }
}

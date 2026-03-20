using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactReadRepository(AppDbContext dbContext) : IContactReadRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id.Value == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Contacts
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
        return await _dbContext.Contacts
            .AnyAsync(
                x => x.OwnerUserId == ownerUserId && x.ContactUserId == contactUserId,
                cancellationToken);
    }

    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contacts
            .FirstOrDefaultAsync(x => x.Id.Value == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

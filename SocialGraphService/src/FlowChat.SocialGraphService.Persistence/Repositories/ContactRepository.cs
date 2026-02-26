using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public class ContactRepository : RepositoryBase<Contact>, IContactRepository
{
    public ContactRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        InvitationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Contacts
            .AsNoTracking()
            .Where(x => x.UserId1 == userId || x.UserId2 == userId);

        if (status.HasValue)
        {
            query = status.Value switch
            {
                InvitationStatus.Accepted => query.Where(x => !x.IsBlocked),
                // InvitationStatus.Canceled => query.Where(x => x.IsBlocked),
                _ => query
            };
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RelationshipExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default)
    {
        var (userId1, userId2) = NormalizePair(userAId, userBId);

        return await DbContext.Contacts
            .AnyAsync(x => x.UserId1 == userId1 && x.UserId2 == userId2, cancellationToken);
    }

    public override async Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Contacts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static (Guid UserId1, Guid UserId2) NormalizePair(Guid userAId, Guid userBId)
    {
        return userAId.CompareTo(userBId) <= 0
            ? (userAId, userBId)
            : (userBId, userAId);
    }
}

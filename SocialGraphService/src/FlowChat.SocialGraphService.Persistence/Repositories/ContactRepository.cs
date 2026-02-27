using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _dbContext;

    public ContactRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        InvitationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Contacts
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

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<bool> RelationshipExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default)
    {
        var (userId1, userId2) = NormalizePair(userAId, userBId);

        return await _dbContext.Contacts
            .AnyAsync(x => x.UserId1 == userId1 && x.UserId2 == userId2, cancellationToken);
    }

    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Contacts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Contacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Contact> AddAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = ToEntity(entity);

        await _dbContext.Contacts.AddAsync(persistenceEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(persistenceEntity);
    }

    public async Task UpdateAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Update(ToEntity(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Remove(ToEntity(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (Guid UserId1, Guid UserId2) NormalizePair(Guid userAId, Guid userBId)
    {
        return userAId.CompareTo(userBId) <= 0
            ? (userAId, userBId)
            : (userBId, userAId);
    }

    private static Contact ToDomain(ContactEntity entity)
    {
        return Contact.Create(
            Id<Contact>.FromGuid(entity.Id),
            entity.UserId1,
            entity.UserId2,
            entity.IsBlocked,
            entity.BlockedBy);
    }

    private static ContactEntity ToEntity(Contact entity)
    {
        return ContactEntity.Create(
            entity.Id.Value,
            entity.UserId1,
            entity.UserId2,
            entity.IsBlocked,
            entity.BlockedBy,
            null,
            entity.CreatedBy,
            entity.CreatedAtUtc,
            entity.LastModifiedBy,
            entity.LastModifiedAtUtc);
    }
}

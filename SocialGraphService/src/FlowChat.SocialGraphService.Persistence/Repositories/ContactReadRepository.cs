using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactReadRepository : IContactReadRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public ContactReadRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : _mapper.Map<Contact>(entity);
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

        return entities.Select(_mapper.Map<Contact>).ToList();
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

        return entity is null ? null : _mapper.Map<Contact>(entity);
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Contacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(_mapper.Map<Contact>).ToList();
    }

    private static (Guid UserId1, Guid UserId2) NormalizePair(Guid userAId, Guid userBId)
    {
        return userAId.CompareTo(userBId) <= 0
            ? (userAId, userBId)
            : (userBId, userAId);
    }

}

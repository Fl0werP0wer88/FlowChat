using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
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
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Contacts
            .AsNoTracking()
            .Where(x => x.OwnerUserId == userId);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(_mapper.Map<Contact>).ToList();
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
}

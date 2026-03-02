using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserSocialGraphRepository : IUserSocialGraphRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public UserSocialGraphRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserSocialGraph?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserSocialGraphs
            .AsNoTracking()
            .Include(x => x.Contacts)
            .Include(x => x.Invitations)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return entity is null ? null : _mapper.Map<UserSocialGraph>(entity);
    }

    public async Task<Contact> AddContactAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = _mapper.Map<ContactEntity>(entity);

        await _dbContext.Contacts.AddAsync(persistenceEntity, cancellationToken);

        return entity;
    }

    public async Task UpdateContactAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Update(_mapper.Map<ContactEntity>(entity));
        await Task.CompletedTask;
    }

    public async Task DeleteContactAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Remove(_mapper.Map<ContactEntity>(entity));
        await Task.CompletedTask;
    }

    public async Task<Invitation> AddInvitationAsync(Guid userSocialGraphId, Invitation entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = _mapper.Map<InvitationEntity>(entity);
        persistenceEntity.UserSocialGraphId = userSocialGraphId;

        await _dbContext.Invitations.AddAsync(persistenceEntity, cancellationToken);

        return entity;
    }

    public async Task UpdateInvitationAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Update(_mapper.Map<InvitationEntity>(entity));
        await Task.CompletedTask;
    }

    public async Task DeleteInvitationAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Remove(_mapper.Map<InvitationEntity>(entity));
        await Task.CompletedTask;
    }
}

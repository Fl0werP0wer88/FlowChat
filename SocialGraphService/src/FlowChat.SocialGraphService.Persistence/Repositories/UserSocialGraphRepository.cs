using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence.Entities;

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

    public async Task<Contact> AddContactAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = _mapper.Map<ContactEntity>(entity);

        await _dbContext.Contacts.AddAsync(persistenceEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task UpdateContactAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Update(_mapper.Map<ContactEntity>(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteContactAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Remove(_mapper.Map<ContactEntity>(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Invitation> AddInvitationAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = _mapper.Map<InvitationEntity>(entity);

        await _dbContext.Invitations.AddAsync(persistenceEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task UpdateInvitationAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Update(_mapper.Map<InvitationEntity>(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteInvitationAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Remove(_mapper.Map<InvitationEntity>(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public InvitationRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Invitation> AddAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invitations.AddAsync(_mapper.Map<InvitationEntity>(entity), cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Update(_mapper.Map<InvitationEntity>(entity));
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Remove(_mapper.Map<InvitationEntity>(entity));
        await Task.CompletedTask;
    }
}

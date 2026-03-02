using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class InvitationReadRepository : IInvitationReadRepository
{
    private const string PendingStatus = nameof(InvitationStatus.Pending);
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public InvitationReadRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<bool> PendingBetweenUsersExistsAsync(
        Guid requesterId,
        Guid addresseeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations.AnyAsync(
            x => x.Status == PendingStatus
                 && x.RequesterId == requesterId
                 && x.AddresseeId == addresseeId,
            cancellationToken);
    }

    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : _mapper.Map<Invitation>(entity);
    }

    public async Task<IReadOnlyList<Invitation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Invitations
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(_mapper.Map<Invitation>).ToList();
    }
}

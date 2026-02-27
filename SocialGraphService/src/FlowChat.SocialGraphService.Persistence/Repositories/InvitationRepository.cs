using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private const string PendingStatus = nameof(InvitationStatus.Pending);
    private readonly AppDbContext _dbContext;

    public InvitationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> PendingBetweenUsersExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations.AnyAsync(
            x => x.Status == PendingStatus
                 && ((x.RequesterId == userAId && x.AddresseeId == userBId)
                     || (x.RequesterId == userBId && x.AddresseeId == userAId)),
            cancellationToken);
    }

    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Invitation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Invitations
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Invitation> AddAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = ToEntity(entity);

        await _dbContext.Invitations.AddAsync(persistenceEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(persistenceEntity);
    }

    public async Task UpdateAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Update(ToEntity(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Invitation entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Remove(ToEntity(entity));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Invitation ToDomain(InvitationEntity entity)
    {
        if (!Enum.TryParse<InvitationStatus>(entity.Status, true, out var status))
        {
            throw new InvalidOperationException($"Unsupported invitation status '{entity.Status}'.");
        }

        return new Invitation(
            entity.Id,
            entity.RequesterId,
            entity.AddresseeId,
            status,
            entity.RespondedAtUtc);
    }

    private static InvitationEntity ToEntity(Invitation entity)
    {
        return new InvitationEntity
        {
            Id = entity.Id,
            RequesterId = entity.RequesterId,
            AddresseeId = entity.AddresseeId,
            Status = entity.Status.ToString(),
            RespondedAtUtc = entity.RespondedAtUtc,
            CreatedBy = entity.CreatedBy,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastModifiedBy = entity.LastModifiedBy,
            LastModifiedAtUtc = entity.LastModifiedAtUtc
        };
    }
}

using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class UserPresencePreferencesReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase, IUserPresencePreferencesReadRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<UserPresencePreferencesDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Query()
            .FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<UserPresencePreferencesDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await Query()
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<PresenceStatus?> FindPreferredStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var preference = await GetByIdAsync(userId, cancellationToken);
        return preference?.PreferredStatus;
    }

    private IQueryable<UserPresencePreferencesReadEntity> Query()
    {
        return Active(_dbContext.UserPresencePreferenceReads);
    }

    private static UserPresencePreferencesDto MapToDto(UserPresencePreferencesReadEntity entity)
    {
        return new UserPresencePreferencesDto(
            entity.UserId,
            entity.PreferredStatus,
            entity.LastModifiedAtUtc);
    }
}

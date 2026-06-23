using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class UserPresencePreferencesReadRepository(AppDbContext dbContext)
    : IUserPresencePreferencesReadRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<UserPresencePreferencesDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var typedId = Id<UserPresencePreferences>.FromGuid(id);
        var entity = await Query()
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);

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

    private IQueryable<UserPresencePreferences> Query()
    {
        return _dbContext.UserPresencePreferences
            .AsNoTracking();
    }

    private static UserPresencePreferencesDto MapToDto(UserPresencePreferences entity)
    {
        return new UserPresencePreferencesDto(
            entity.UserId,
            entity.PreferredStatus,
            entity.LastModifiedAtUtc.Value);
    }
}

using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Persistence.Entities;

namespace FlowChat.PresenceService.Persistence.Repositories;

internal sealed class UserPresencePreferencesRepository(AppDbContext context)
    : IUserPresencePreferencesRepository
{
    public async Task<PresenceStatus?> FindPreferredStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await context.UserPresencePreferences.FindAsync([userId], cancellationToken);
        return entity?.PreferredStatus;
    }

    public async Task UpsertAsync(Guid userId, PresenceStatus status, DateTimeOffset lastModifiedAtUtc, CancellationToken cancellationToken)
    {
        var existing = await context.UserPresencePreferences.FindAsync([userId], cancellationToken);
        if (existing is not null)
        {
            existing.PreferredStatus = status;
            existing.LastModifiedAtUtc = lastModifiedAtUtc;
        }
        else
        {
            context.UserPresencePreferences.Add(new UserPresencePreferencesEntity
            {
                UserId = userId,
                PreferredStatus = status,
                LastModifiedAtUtc = lastModifiedAtUtc
            });
        }
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await context.UserPresencePreferences.FindAsync([userId], cancellationToken);
        if (existing is not null)
        {
            context.UserPresencePreferences.Remove(existing);
        }
    }
}

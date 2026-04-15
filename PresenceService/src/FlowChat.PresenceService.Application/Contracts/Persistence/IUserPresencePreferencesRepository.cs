using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IUserPresencePreferencesRepository
{
    Task<PresenceStatus?> FindPreferredStatusAsync(Guid userId, CancellationToken cancellationToken);

    Task UpsertAsync(Guid userId, PresenceStatus status, DateTimeOffset lastModifiedAtUtc, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);
}

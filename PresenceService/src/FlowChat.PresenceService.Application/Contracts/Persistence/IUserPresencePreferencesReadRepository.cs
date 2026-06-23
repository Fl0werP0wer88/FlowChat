using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.Shared.Application;

namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IUserPresencePreferencesReadRepository : IReadRepository<UserPresencePreferencesDto>
{
    Task<PresenceStatus?> FindPreferredStatusAsync(Guid userId, CancellationToken cancellationToken = default);
}

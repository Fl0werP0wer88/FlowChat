using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IPresenceInternalApiClient
{
    Task RefreshPresenceStatusAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ContactPresenceStatusDto>> GetContactPresenceStatusesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<PresenceStatus?> GetUserPresencePreferencesAsync(Guid userId, CancellationToken cancellationToken);
}

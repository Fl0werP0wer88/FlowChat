using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Features.Presence;

namespace FlowChat.PresenceService.Application.Contracts.Infrastructure;

public interface IPresenceStatusStore
{
    Task<PresenceStatusSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task SetAsync(
        Guid userId,
        PresenceStatus status,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);
}

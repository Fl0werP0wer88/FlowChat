using FlowChat.Core.Domain;

namespace FlowChat.GatewayService.Infrastructure.Clients.PresenceService;

public sealed record ContactPresenceStatusClientDto(
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset ChangedAtUtc);

public interface IPresenceServiceClient
{
    Task<IReadOnlyDictionary<Guid, ContactPresenceStatusClientDto>> GetPresenceStatusesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}

using FlowChat.PresenceService.Consumers.Presence.Contracts;

namespace FlowChat.PresenceService.Consumers.Services;

public interface IPresenceInternalApiClient
{
    Task BulkUpsertOrDeleteUserContactProjectionAsync(
        BulkUpsertOrDeleteUserContactProjectionRequest request,
        CancellationToken cancellationToken);
}

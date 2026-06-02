using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public interface ISocialGraphInternalApiClient
{
    Task BulkUpsertOrDeleteUserProfileProjectionAsync(
        BulkUpsertOrDeleteUserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

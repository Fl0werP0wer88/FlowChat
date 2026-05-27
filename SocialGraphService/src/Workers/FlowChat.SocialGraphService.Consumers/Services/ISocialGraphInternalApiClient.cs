using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public interface ISocialGraphInternalApiClient
{
    Task BulkUpsertUserProfileProjectionAsync(
        BulkUpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

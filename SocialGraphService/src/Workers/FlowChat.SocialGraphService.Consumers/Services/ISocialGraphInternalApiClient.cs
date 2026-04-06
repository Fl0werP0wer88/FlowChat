using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public interface ISocialGraphInternalApiClient
{
    Task UpsertUserProfileProjectionAsync(
        UpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

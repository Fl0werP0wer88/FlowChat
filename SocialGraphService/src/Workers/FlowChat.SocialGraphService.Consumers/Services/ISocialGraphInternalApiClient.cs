using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public interface ISocialGraphInternalApiClient
{
    Task InsertUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken);

    Task UpdateUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

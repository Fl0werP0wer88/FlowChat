using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public interface ISocialGraphInternalApiClient
{
    Task UpsertUserProfileReadModelAsync(
        UpsertUserProfileReadModelRequest request,
        CancellationToken cancellationToken);
}

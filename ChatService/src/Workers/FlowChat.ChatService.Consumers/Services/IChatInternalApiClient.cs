using FlowChat.ChatService.Consumers.ChatService.Contracts;

namespace FlowChat.ChatService.Consumers.Services;

public interface IChatInternalApiClient
{
    Task InsertUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken);

    Task UpdateUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

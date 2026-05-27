using FlowChat.ChatService.Consumers.ChatService.Contracts;

namespace FlowChat.ChatService.Consumers.Services;

public interface IChatInternalApiClient
{
    Task BulkUpsertUserProfileProjectionAsync(
        BulkUpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

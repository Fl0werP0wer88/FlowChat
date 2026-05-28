using FlowChat.ChatService.Consumers.ChatService.Contracts;

namespace FlowChat.ChatService.Consumers.Services;

public interface IChatInternalApiClient
{
    Task BulkUpsertOrDeleteUserProfileProjectionAsync(
        BulkUpsertOrDeleteUserProfileProjectionRequest request,
        CancellationToken cancellationToken);
}

using FlowChat.HarnessService.Consumers.Services.Projections.Contracts;

namespace FlowChat.HarnessService.Consumers.Services;

public interface IHarnessApiClient
{
    Task BulkUpsertProjectionAsync(
        BulkUpsertProjectionRequest request,
        CancellationToken cancellationToken);
}

using System.Net.Http.Json;
using FlowChat.HarnessService.Consumers.Services.Projections.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.HarnessService.Consumers.Services;

public sealed class HarnessApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IHarnessApiClient
{
    private const string BulkUpsertPath = "/internal/projections/bulk-upsert";

    protected override string ClientDisplayName => "HarnessService API";

    public Task BulkUpsertProjectionAsync(
        BulkUpsertProjectionRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            new HttpRequestMessage(HttpMethod.Post, BulkUpsertPath)
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken);
}

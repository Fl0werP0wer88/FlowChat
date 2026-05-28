using System.Net.Http.Json;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.ChatService.Consumers.Services;

public sealed class ChatInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatInternalApiClient
{
    public const string HttpClientName = nameof(ChatInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BulkUpsertOrDeletePath = "/internal/userprofiles/projection/bulk-upsert-or-delete";

    protected override string ClientDisplayName => "Chat API";

    public Task BulkUpsertOrDeleteUserProfileProjectionAsync(
        BulkUpsertOrDeleteUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendAsync(BuildRequest(HttpMethod.Post, BulkUpsertOrDeletePath, request), cancellationToken);

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, BulkUpsertOrDeleteUserProfileProjectionRequest request) =>
        new(method, path) { Content = JsonContent.Create(request) };
}

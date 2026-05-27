using System.Net.Http.Json;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.ChatService.Consumers.Services;

public sealed class ChatInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatInternalApiClient
{
    public const string HttpClientName = nameof(ChatInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BulkUpsertPath = "/internal/userprofiles/projection/bulk-upsert";

    protected override string ClientDisplayName => "Chat API";

    public Task BulkUpsertUserProfileProjectionAsync(
        BulkUpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendAsync(BuildRequest(HttpMethod.Post, BulkUpsertPath, request), cancellationToken);

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, BulkUpsertUserProfileProjectionRequest request) =>
        new(method, path) { Content = JsonContent.Create(request) };
}


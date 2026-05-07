using System.Net.Http.Json;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.ChatService.Consumers.Services;

public sealed class ChatInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatInternalApiClient
{
    public const string HttpClientName = nameof(ChatInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string InsertPath = "/internal/userprofiles/projection/insert";
    private const string UpdatePath = "/internal/userprofiles/projection/update";

    protected override string ClientDisplayName => "Chat API";

    public Task InsertUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendAsync(BuildRequest(HttpMethod.Post, InsertPath, request), cancellationToken);

    public Task UpdateUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendAsync(BuildRequest(HttpMethod.Put, UpdatePath, request), cancellationToken);

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, UserProfileProjectionRequest request) =>
        new(method, path) { Content = JsonContent.Create(request) };
}


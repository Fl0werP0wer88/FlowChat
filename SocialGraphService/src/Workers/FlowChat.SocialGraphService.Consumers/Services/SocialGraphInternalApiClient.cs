using System.Net;
using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public sealed class SocialGraphInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), ISocialGraphInternalApiClient
{
    public const string HttpClientName = nameof(SocialGraphInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BulkUpsertUserProfileProjectionPath = "/internal/userprofiles/projection/bulk-upsert";

    protected override string ClientDisplayName => "SocialGraph API";

    public Task BulkUpsertUserProfileProjectionAsync(
        BulkUpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendUserProfileProjectionAsync(HttpMethod.Post, BulkUpsertUserProfileProjectionPath, request, cancellationToken);

    private async Task SendUserProfileProjectionAsync(
        HttpMethod method,
        string path,
        BulkUpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}


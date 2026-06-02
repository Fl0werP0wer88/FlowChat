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
    private const string BulkUpsertOrDeleteUserProfileProjectionPath = "/internal/userprofiles/projection/bulk-upsert-or-delete";

    protected override string ClientDisplayName => "SocialGraph API";

    public Task BulkUpsertOrDeleteUserProfileProjectionAsync(
        BulkUpsertOrDeleteUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendUserProfileProjectionAsync(HttpMethod.Post, BulkUpsertOrDeleteUserProfileProjectionPath, request, cancellationToken);

    private async Task SendUserProfileProjectionAsync(
        HttpMethod method,
        string path,
        BulkUpsertOrDeleteUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}


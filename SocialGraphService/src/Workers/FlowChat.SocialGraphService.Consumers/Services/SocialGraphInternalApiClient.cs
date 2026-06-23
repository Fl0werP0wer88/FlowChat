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
    private const string InsertUserProfileProjectionPath = "/internal/userprofiles/projection/insert";
    private const string UpdateUserProfileProjectionPath = "/internal/userprofiles/projection/update";

    protected override string ClientDisplayName => "SocialGraph API";

    public Task InsertUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendUserProfileProjectionAsync(HttpMethod.Post, InsertUserProfileProjectionPath, request, cancellationToken);

    public Task UpdateUserProfileProjectionAsync(
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
        => SendUserProfileProjectionAsync(HttpMethod.Put, UpdateUserProfileProjectionPath, request, cancellationToken);

    private async Task SendUserProfileProjectionAsync(
        HttpMethod method,
        string path,
        UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}


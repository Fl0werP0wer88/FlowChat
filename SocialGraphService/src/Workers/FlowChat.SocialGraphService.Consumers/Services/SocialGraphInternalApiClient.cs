using System.Net;
using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Services;

public sealed class SocialGraphInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), ISocialGraphInternalApiClient
{
    public const string HttpClientName = nameof(SocialGraphInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string UpsertUserProfileReadModelPath = "/internal/userprofiles/read-model";

    protected override string ClientDisplayName => "SocialGraph API";

    public async Task UpsertUserProfileReadModelAsync(
        UpsertUserProfileReadModelRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, UpsertUserProfileReadModelPath)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

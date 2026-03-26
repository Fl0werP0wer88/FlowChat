using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Exceptions;
using FlowChat.SocialGraphService.Consumers.Configuration;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.Consumers.Services;

public sealed class SocialGraphInternalApiClient(
    HttpClient httpClient,
    IOptions<SocialGraphApiSettings> apiSettings)
    : ISocialGraphInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string UpsertUserProfileReadModelPath = "/internal/userprofiles/read-model";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly SocialGraphApiSettings _apiSettings = apiSettings?.Value
        ?? throw new ArgumentNullException(nameof(apiSettings));

    public async Task UpsertUserProfileReadModelAsync(
        UpsertUserProfileReadModelRequest request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_apiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException("SocialGraphApi:BaseUrl must be an absolute URI.");
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(baseAddress, UpsertUserProfileReadModelPath))
        {
            Content = JsonContent.Create(request)
        };

        if (!string.IsNullOrWhiteSpace(_apiSettings.ApiKey))
        {
            message.Headers.Add(ApiKeyHeaderName, _apiSettings.ApiKey);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.Unauthorized)
        {
            throw new NonTransientException(await BuildFailureMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> BuildFailureMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var suffix = string.IsNullOrWhiteSpace(body)
            ? string.Empty
            : $": {body.Trim()}";

        return $"SocialGraph API returned {(int)response.StatusCode} {response.ReasonPhrase}{suffix}";
    }
}

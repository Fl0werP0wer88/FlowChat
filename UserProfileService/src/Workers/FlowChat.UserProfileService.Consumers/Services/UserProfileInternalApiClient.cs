using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Exceptions;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

namespace FlowChat.UserProfileService.Consumers.Services;

public sealed class UserProfileInternalApiClient(HttpClient httpClient) : IUserProfileInternalApiClient
{
    public const string HttpClientName = nameof(UserProfileInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string CreateInitialUserProfilePath = "/internal/userprofiles/initial";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task CreateInitialUserProfileAsync(
        CreateInitialUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, CreateInitialUserProfilePath)
        {
            Content = JsonContent.Create(request)
        };

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

        return $"UserProfile API returned {(int)response.StatusCode} {response.ReasonPhrase}{suffix}";
    }
}

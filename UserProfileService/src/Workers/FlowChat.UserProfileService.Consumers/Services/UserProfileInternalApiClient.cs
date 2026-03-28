using System.Net;
using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

namespace FlowChat.UserProfileService.Consumers.Services;

public sealed class UserProfileInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IUserProfileInternalApiClient
{
    public const string HttpClientName = nameof(UserProfileInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string CreateInitialUserProfilePath = "/internal/userprofiles/initial";

    protected override string ClientDisplayName => "UserProfile API";

    public async Task CreateInitialUserProfileAsync(
        CreateInitialUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, CreateInitialUserProfilePath)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

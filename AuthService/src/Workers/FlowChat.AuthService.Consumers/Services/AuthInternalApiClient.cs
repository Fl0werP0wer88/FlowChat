using System.Net.Http.Json;
using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.AuthService.Consumers.Services;

public sealed class AuthInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IAuthInternalApiClient
{
    public const string HttpClientName = nameof(AuthInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ConfirmEmailPath = "/internal/users/email-confirmation";

    protected override string ClientDisplayName => "Auth API";

    public async Task ConfirmEmailAsync(
        AuthEmailConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ConfirmEmailPath)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

using System.Net;
using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;

namespace FlowChat.NotificationService.Consumers.Services;

public sealed class NotificationInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), INotificationInternalApiClient
{
    public const string HttpClientName = nameof(NotificationInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ProcessUserEmailVerificationRequestedPath = "/internal/notifications/email-verification-requested";

    protected override string ClientDisplayName => "Notification API";

    public async Task ProcessUserEmailVerificationRequestedAsync(
        ProcessUserEmailVerificationRequestedRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ProcessUserEmailVerificationRequestedPath)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

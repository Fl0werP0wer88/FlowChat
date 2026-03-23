using System.Net;
using System.Net.Http.Json;
using FlowChat.NotificationService.Consumers.Configuration;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using Microsoft.Extensions.Options;

namespace FlowChat.NotificationService.Consumers.Services;

public sealed class NotificationInternalApiClient(
    HttpClient httpClient,
    IOptions<NotificationApiSettings> apiSettings)
    : INotificationInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ProcessUserEmailVerificationRequestedPath = "/internal/notifications/email-verification-requested";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly NotificationApiSettings _apiSettings = apiSettings?.Value
        ?? throw new ArgumentNullException(nameof(apiSettings));

    public async Task ProcessUserEmailVerificationRequestedAsync(
        ProcessUserEmailVerificationRequestedRequest request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_apiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException("NotificationApi:BaseUrl must be an absolute URI.");
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(baseAddress, ProcessUserEmailVerificationRequestedPath))
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
            throw new InvalidOperationException(await BuildFailureMessageAsync(response, cancellationToken));
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

        return $"Notification API returned {(int)response.StatusCode} {response.ReasonPhrase}{suffix}";
    }
}

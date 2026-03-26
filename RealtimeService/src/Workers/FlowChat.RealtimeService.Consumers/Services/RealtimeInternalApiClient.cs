using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Exceptions;
using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;

namespace FlowChat.RealtimeService.Consumers.Services;

public sealed class RealtimeInternalApiClient(HttpClient httpClient, IConsumersSettingsManager settingsManager)
    : IRealtimeInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ReceiveMessagePath = "/internal/realtime/messages";
    private const string PresenceChangedPath = "/internal/realtime/presence";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly RealtimeApiSettings _settings = settingsManager?.GetRealtimeApiSettings()
        ?? throw new ArgumentNullException(nameof(settingsManager));

    public Task PublishMessageAsync(PublishMessageRequest request, CancellationToken cancellationToken) =>
        PostAsync(ReceiveMessagePath, request, cancellationToken);

    public Task PublishPresenceChangeAsync(PublishPresenceChangeRequest request, CancellationToken cancellationToken) =>
        PostAsync(PresenceChangedPath, request, cancellationToken);

    private async Task PostAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_settings.BaseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException("RealtimeApi:BaseUrl must be an absolute URI.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseAddress, path))
        {
            Content = JsonContent.Create(request)
        };

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            message.Headers.Add(ApiKeyHeaderName, _settings.ApiKey);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
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

        return $"Realtime API returned {(int)response.StatusCode} {response.ReasonPhrase}{suffix}";
    }
}

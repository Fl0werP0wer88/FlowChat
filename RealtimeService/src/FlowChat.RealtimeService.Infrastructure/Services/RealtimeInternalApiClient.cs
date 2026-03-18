using System.Net.Http.Json;
using FlowChat.RealtimeService.Application.Realtime.Contracts;
using FlowChat.RealtimeService.Infrastructure.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Services;

public sealed class RealtimeInternalApiClient(HttpClient httpClient, IApiSettingsManager apiSettingsManager)
    : IRealtimeInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ReceiveMessagePath = "/internal/realtime/messages";
    private const string PresenceChangedPath = "/internal/realtime/presence";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly RealtimeApiSettings _settings = apiSettingsManager?.GetRealtimeApiSettings()
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

    public Task ForwardReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken) =>
        PostAsync(ReceiveMessagePath, request, cancellationToken);

    public Task ForwardPresenceChangedAsync(PresenceChangedRequest request, CancellationToken cancellationToken) =>
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
        response.EnsureSuccessStatusCode();
    }
}

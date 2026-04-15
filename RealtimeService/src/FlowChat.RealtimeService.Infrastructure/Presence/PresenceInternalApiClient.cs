using System.Net.Http.Json;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.Presence;

internal sealed class PresenceInternalApiClient(IHttpClientFactory httpClientFactory) : IPresenceInternalApiClient
{
    public const string HttpClientName = nameof(PresenceInternalApiClient);
    private const string RefreshPath = "/internal/presence/status/refresh";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory
        ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public async Task RefreshPresenceStatusAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(HttpMethod.Post, RefreshPath)
        {
            Content = JsonContent.Create(new { userIds })
        };
        using var response = await client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

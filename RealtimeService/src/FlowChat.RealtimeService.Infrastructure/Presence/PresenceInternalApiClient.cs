using System.Net.Http.Json;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.Presence;

internal sealed class PresenceInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IPresenceInternalApiClient
{
    private const string InitializePath = "/internal/presence/status/initialize";
    private const string DeletePath = "/internal/presence/status/delete";
    private const string RefreshPath = "/internal/presence/status/refresh";

    protected override string ClientDisplayName => "Presence Service";

    public async Task InitializePresenceStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, InitializePath)
        {
            Content = JsonContent.Create(new { userId })
        };
        await SendAsync(request, cancellationToken);
    }

    public async Task DeletePresenceStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, DeletePath)
        {
            Content = JsonContent.Create(new { userId })
        };
        await SendAsync(request, cancellationToken);
    }

    public async Task RefreshPresenceStatusAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath)
        {
            Content = JsonContent.Create(new { userIds })
        };
        await SendAsync(request, cancellationToken);
    }
}

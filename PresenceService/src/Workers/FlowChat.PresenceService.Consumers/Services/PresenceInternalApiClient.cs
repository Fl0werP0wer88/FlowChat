using System.Net.Http.Json;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.PresenceService.Consumers.Services;

public sealed class PresenceInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IPresenceInternalApiClient
{
    public const string HttpClientName = nameof(PresenceInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BulkUpsertPath = "/internal/presence/contact-observers/bulk-upsert";
    private const string DeletePath = "/internal/presence/contact-observers/delete";

    protected override string ClientDisplayName => "Presence API";

    public Task BulkUpsertContactObserverProjectionAsync(
        BulkUpsertContactObserverProjectionRequest request,
        CancellationToken cancellationToken) =>
        SendContactObserverProjectionAsync(HttpMethod.Post, BulkUpsertPath, request, cancellationToken);

    public Task DeleteContactObserverProjectionAsync(
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken) =>
        SendContactObserverProjectionAsync(HttpMethod.Delete, DeletePath, request, cancellationToken);

    private async Task SendContactObserverProjectionAsync(
        HttpMethod method,
        string path,
        object request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}


using System.Net.Http.Json;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.PresenceService.Consumers.Services;

public sealed class PresenceInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IPresenceInternalApiClient
{
    public const string HttpClientName = nameof(PresenceInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string InitializePresencePath = "/internal/presence/status/initialize";
    private const string DeletePresencePath = "/internal/presence/status/delete";
    private const string InsertPath = "/internal/presence/contact-observers/insert";
    private const string DeletePath = "/internal/presence/contact-observers/delete";

    protected override string ClientDisplayName => "Presence API";

    public Task InitializePresenceStatusAsync(
        PresenceStatusRequest request,
        CancellationToken cancellationToken) =>
        SendPresenceStatusAsync(HttpMethod.Post, InitializePresencePath, request, cancellationToken);

    public Task DeletePresenceStatusAsync(
        PresenceStatusRequest request,
        CancellationToken cancellationToken) =>
        SendPresenceStatusAsync(HttpMethod.Delete, DeletePresencePath, request, cancellationToken);

    public Task InsertContactObserverProjectionAsync(
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken) =>
        SendContactObserverProjectionAsync(HttpMethod.Post, InsertPath, request, cancellationToken);

    public Task DeleteContactObserverProjectionAsync(
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken) =>
        SendContactObserverProjectionAsync(HttpMethod.Delete, DeletePath, request, cancellationToken);

    private async Task SendContactObserverProjectionAsync(
        HttpMethod method,
        string path,
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }

    private async Task SendPresenceStatusAsync(
        HttpMethod method,
        string path,
        PresenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

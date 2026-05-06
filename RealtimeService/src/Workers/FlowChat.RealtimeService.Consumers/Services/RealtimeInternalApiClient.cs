using System.Net;
using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;

namespace FlowChat.RealtimeService.Consumers.Services;

public sealed class RealtimeInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IRealtimeInternalApiClient
{
    public const string HttpClientName = nameof(RealtimeInternalApiClient);
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ReceiveMessagePath = "/internal/realtime/messages";
    private const string PresenceChangedPath = "/internal/realtime/presence";

    protected override string ClientDisplayName => "Realtime API";

    public Task PublishMessageAsync(PublishMessageRequest request, CancellationToken cancellationToken) =>
        PostAsync(ReceiveMessagePath, request, cancellationToken);

    public Task PublishPresenceChangeAsync(
        PublishPresenceChangeRequest request,
        CancellationToken cancellationToken) =>
        PostAsync(PresenceChangedPath, request, cancellationToken);

    private async Task PostAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

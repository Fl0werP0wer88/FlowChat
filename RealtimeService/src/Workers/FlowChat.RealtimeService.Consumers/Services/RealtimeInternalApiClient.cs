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

    public Task PublishMessageAsync(Uri baseAddress, PublishMessageRequest request, CancellationToken cancellationToken) =>
        PostAsync(baseAddress, ReceiveMessagePath, request, cancellationToken);

    public Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PublishPresenceChangeRequest request,
        CancellationToken cancellationToken) =>
        PostAsync(baseAddress, PresenceChangedPath, request, cancellationToken);

    private async Task PostAsync<TRequest>(Uri baseAddress, string path, TRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseAddress, path))
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }
}

using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.GatewayService.Infrastructure.Clients.PresenceService;

internal sealed class PresenceServiceClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IPresenceServiceClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BatchStatusesPath = "internal/presence/statuses/batch";

    private sealed record PresenceStatusesClientRequest(IReadOnlyCollection<Guid> UserIds);

    protected override string ClientDisplayName => "Presence Service";

    public async Task<IReadOnlyDictionary<Guid, ContactPresenceStatusClientDto>> GetPresenceStatusesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BatchStatusesPath)
        {
            Content = JsonContent.Create(new PresenceStatusesClientRequest(userIds))
        };

        var statuses = await SendAsync<IReadOnlyCollection<ContactPresenceStatusClientDto>>(
            request,
            cancellationToken);

        return (statuses ?? [])
            .GroupBy(status => status.UserId)
            .ToDictionary(group => group.Key, group => group.First());
    }
}

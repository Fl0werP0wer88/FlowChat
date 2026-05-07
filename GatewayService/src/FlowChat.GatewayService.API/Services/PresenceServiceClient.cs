using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class PresenceServiceClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IPresenceServiceClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BatchStatusesPath = "internal/presence/statuses/batch";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

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
            Content = JsonContent.Create(new PresenceStatusesClientRequest(userIds), options: JsonOptions)
        };

        var statuses = await SendAsync<IReadOnlyCollection<ContactPresenceStatusClientDto>>(
            request,
            JsonOptions,
            cancellationToken);

        return (statuses ?? [])
            .GroupBy(status => status.UserId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

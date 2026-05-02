using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class PresenceServiceClient(HttpClient httpClient) : IPresenceServiceClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string BatchStatusesPath = "internal/presence/statuses/batch";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private sealed record PresenceStatusesClientRequest(IReadOnlyCollection<Guid> UserIds);

    public async Task<IReadOnlyDictionary<Guid, ContactPresenceStatusClientDto>> GetPresenceStatusesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }

        using var response = await httpClient.PostAsJsonAsync(
            BatchStatusesPath,
            new PresenceStatusesClientRequest(userIds),
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var statuses = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<ContactPresenceStatusClientDto>>(
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

using System.Net.Http.Json;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class ChatServiceClient : IChatServiceClient
{
    private sealed record DuetConversationIdsClientRequest(IReadOnlyList<Guid> PartnerUserIds);

    private sealed record DuetConversationIdsClientResponse(IReadOnlyDictionary<Guid, Guid> ConversationIds);

    private readonly HttpClient _httpClient;

    public ChatServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetDuetConversationIdsAsync(
        IReadOnlyList<Guid> partnerUserIds,
        CancellationToken cancellationToken)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync(
            "api/conversations/duet/batch",
            new DuetConversationIdsClientRequest(partnerUserIds),
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<DuetConversationIdsClientResponse>(cancellationToken);
        return response?.ConversationIds ?? new Dictionary<Guid, Guid>();
    }
}

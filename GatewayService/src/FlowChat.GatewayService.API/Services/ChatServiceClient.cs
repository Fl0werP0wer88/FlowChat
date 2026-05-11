using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class ChatServiceClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatServiceClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private sealed record CreateDuetConversationClientRequest(Guid PartnerUserId);

    private sealed record DuetConversationIdsClientRequest(IReadOnlyList<Guid> PartnerUserIds);

    private sealed record DuetConversationIdsClientResponse(IReadOnlyDictionary<Guid, Guid> ConversationIds);

    protected override string ClientDisplayName => "Chat Service";

    public async Task<DuetConversationClientDto?> GetDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        var encodedPartnerUserId = Uri.EscapeDataString(partnerUserId.ToString());
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/conversations/duet/detail?partnerUserId={encodedPartnerUserId}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DuetConversationClientDto>(JsonOptions, cancellationToken);
    }

    public async Task<DuetConversationClientDto> CreateDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/conversations/duet")
        {
            Content = JsonContent.Create(new CreateDuetConversationClientRequest(partnerUserId))
        };

        var response = await SendAsync<DuetConversationClientDto>(request, cancellationToken);
        return response ?? throw new InvalidOperationException("Chat Service returned an empty duet conversation response.");
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetDuetConversationIdsAsync(
        IReadOnlyList<Guid> partnerUserIds,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/conversations/duet/batch")
        {
            Content = JsonContent.Create(new DuetConversationIdsClientRequest(partnerUserIds))
        };

        var response = await SendAsync<DuetConversationIdsClientResponse>(request, cancellationToken);
        return response?.ConversationIds ?? new Dictionary<Guid, Guid>();
    }

    public async Task<ChatMessagesClientDto> GetConversationMessagesAsync(
        Guid conversationId,
        int limit,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/chat/conversations/{conversationId}/messages?limit={limit}");

        var response = await SendAsync<ChatMessagesClientDto>(request, cancellationToken);
        return response ?? new ChatMessagesClientDto([], null, null, false);
    }
}

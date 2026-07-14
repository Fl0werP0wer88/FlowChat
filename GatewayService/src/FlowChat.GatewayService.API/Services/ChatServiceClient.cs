using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class ChatServiceClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatServiceClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private sealed record CreateDuetConversationClientRequest(Guid PartnerUserId);

    private sealed record ContactsClientResponse(IReadOnlyList<ContactClientDto> Contacts);

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

    public async Task<IReadOnlyList<ContactClientDto>> GetContactsForUserAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/conversations/contacts");
        var response = await SendAsync<ContactsClientResponse>(request, cancellationToken);
        return response?.Contacts ?? [];
    }

    public async Task<GroupConversationClientDto?> GetGroupConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/conversations/group/{conversationId}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GroupConversationClientDto>(JsonOptions, cancellationToken);
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

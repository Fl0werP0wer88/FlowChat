using System.Net.Http.Json;
using FlowChat.Core.Results;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.Shared.Domain;

namespace FlowChat.GatewayService.Infrastructure.Clients.ChatService;

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

    public Task<FlowChatResult<ConversationMessagesRangeClientDto>> GetConversationMessagesRangeAscendingAsync(
        Guid conversationId,
        Guid requestingUserId,
        long? startSequenceNum,
        long? endSequenceNum,
        int limit,
        CancellationToken cancellationToken) =>
        GetConversationMessagesRangeAsync(
            conversationId,
            requestingUserId,
            startSequenceNum,
            endSequenceNum,
            limit,
            "ascending",
            cancellationToken);

    public Task<FlowChatResult<ConversationMessagesRangeClientDto>> GetConversationMessagesRangeDescendingAsync(
        Guid conversationId,
        Guid requestingUserId,
        long? startSequenceNum,
        long? endSequenceNum,
        int limit,
        CancellationToken cancellationToken) =>
        GetConversationMessagesRangeAsync(
            conversationId,
            requestingUserId,
            startSequenceNum,
            endSequenceNum,
            limit,
            "descending",
            cancellationToken);

    private async Task<FlowChatResult<ConversationMessagesRangeClientDto>> GetConversationMessagesRangeAsync(
        Guid conversationId,
        Guid requestingUserId,
        long? startSequenceNum,
        long? endSequenceNum,
        int limit,
        string direction,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>
        {
            $"requestingUserId={Uri.EscapeDataString(requestingUserId.ToString())}",
            $"limit={limit}"
        };
        if (startSequenceNum.HasValue)
        {
            parameters.Add($"startSequenceNum={startSequenceNum.Value}");
        }
        if (endSequenceNum.HasValue)
        {
            parameters.Add($"endSequenceNum={endSequenceNum.Value}");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"internal/chat/conversations/{conversationId}/messages/range/{direction}?{string.Join("&", parameters)}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var page = await response.Content.ReadFromJsonAsync<ConversationMessagesRangeClientDto>(
                JsonOptions,
                cancellationToken);
            return page is null
                ? FlowChatResult<ConversationMessagesRangeClientDto>.Failure(
                    DomainError.UnExpected("Chat Service returned an empty message range response."))
                : FlowChatResult<ConversationMessagesRangeClientDto>.Success(page);
        }

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest =>
                FlowChatResult<ConversationMessagesRangeClientDto>.Failure(
                    DomainError.BadRequest("Chat Service rejected the message range.")),
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                FlowChatResult<ConversationMessagesRangeClientDto>.Failure(
                    DomainError.Unauthorized("Message range access was denied.")),
            System.Net.HttpStatusCode.NotFound =>
                FlowChatResult<ConversationMessagesRangeClientDto>.Failure(
                    DomainError.NotFound("Conversation not found.")),
            _ => FlowChatResult<ConversationMessagesRangeClientDto>.Failure(
                DomainError.UnExpected(
                    BuildFailureMessage(
                        response,
                        response.Content is null
                            ? null
                            : await response.Content.ReadAsStringAsync(cancellationToken))))
        };
    }

}

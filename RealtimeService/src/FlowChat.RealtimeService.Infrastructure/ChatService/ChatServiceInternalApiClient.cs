using System.Net.Http.Json;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.ChatService;

internal sealed class ChatServiceInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatServiceInternalApiClient
{
    protected override string ClientDisplayName => "Chat Service";

    public async Task MarkChatMessageAsDeliveredAsync(Guid messageId, Guid conversationId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/internal/messages/{messageId}/delivery")
        {
            Content = JsonContent.Create(new { ConversationId = conversationId })
        };
        await SendAsync(request, cancellationToken);
    }
}

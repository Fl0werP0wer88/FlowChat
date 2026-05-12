using System.Net.Http.Json;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.ChatService;

internal sealed class ChatServiceInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatServiceInternalApiClient
{
    protected override string ClientDisplayName => "Chat Service";

    public async Task MarkMessageProcessedAsync(Guid messageId, Guid conversationId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/internal/messages/{messageId}/process")
        {
            Content = JsonContent.Create(new { ConversationId = conversationId })
        };
        await SendAsync(request, cancellationToken);
    }
}

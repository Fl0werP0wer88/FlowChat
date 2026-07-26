using System.Net.Http.Json;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.InternalApis.ChatService;

internal sealed class ChatServiceInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatServiceInternalApiClient
{
    protected override string ClientDisplayName => "Chat Service";

    public async Task MarkChatMessageAsDeliveredAsync(
        Guid messageId,
        Guid conversationId,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/internal/messages/{messageId}/delivery")
        {
            Content = JsonContent.Create(new { ConversationId = conversationId, DeliveredAtUtc = deliveredAtUtc })
        };
        await SendAsync(request, cancellationToken);
    }

}

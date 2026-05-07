using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class ChatServiceClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IChatServiceClient
{
    private sealed record DuetConversationIdsClientRequest(IReadOnlyList<Guid> PartnerUserIds);

    private sealed record DuetConversationIdsClientResponse(IReadOnlyDictionary<Guid, Guid> ConversationIds);

    protected override string ClientDisplayName => "Chat Service";

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
}

namespace FlowChat.GatewayService.Api.Services;

public interface IChatServiceClient
{
    Task<DuetConversationClientDto?> GetDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken);

    Task<DuetConversationClientDto> CreateDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Guid>> GetDuetConversationIdsAsync(
        IReadOnlyList<Guid> partnerUserIds,
        CancellationToken cancellationToken);

    Task<ChatMessagesClientDto> GetConversationMessagesAsync(
        Guid conversationId,
        int limit,
        CancellationToken cancellationToken);
}

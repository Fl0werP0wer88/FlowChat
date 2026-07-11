namespace FlowChat.GatewayService.Api.Services;

public interface IChatServiceClient
{
    Task<DuetConversationClientDto?> GetDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken);

    Task<DuetConversationClientDto> CreateDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContactClientDto>> GetContactsForUserAsync(CancellationToken cancellationToken);

    Task<GroupConversationClientDto?> GetGroupConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<ChatMessagesClientDto> GetConversationMessagesAsync(
        Guid conversationId,
        int limit,
        CancellationToken cancellationToken);
}

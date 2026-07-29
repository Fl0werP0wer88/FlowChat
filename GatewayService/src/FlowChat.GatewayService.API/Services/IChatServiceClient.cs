using FlowChat.Core.Results;

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

    Task<FlowChatResult<ConversationMessagesRangeClientDto>> GetConversationMessagesRangeAscendingAsync(
        Guid conversationId,
        Guid requestingUserId,
        long? startSequenceNum,
        long? endSequenceNum,
        int limit,
        CancellationToken cancellationToken);

    Task<FlowChatResult<ConversationMessagesRangeClientDto>> GetConversationMessagesRangeDescendingAsync(
        Guid conversationId,
        Guid requestingUserId,
        long? startSequenceNum,
        long? endSequenceNum,
        int limit,
        CancellationToken cancellationToken);
}

using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IGroupConversationReadRepository
{
    Task<GroupConversationDetailDto?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GroupConversationSummaryDto>> GetByParticipantUserIdAsync(
        Guid participantUserId,
        CancellationToken cancellationToken = default);
}

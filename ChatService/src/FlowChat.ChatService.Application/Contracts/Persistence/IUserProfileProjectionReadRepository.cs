using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionReadRepository
{
    Task<IReadOnlyList<ConversationParticipantDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}

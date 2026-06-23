using FlowChat.ChatService.Application.Features.UserProfile;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionReadRepository
{
    Task<IReadOnlyList<UserProfileConversationParticipantDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}

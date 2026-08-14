using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationV2WriteRepository : IWriteRepository<ConversationV2>
{
    Task<ConversationV2> AddAsync(
        ConversationV2 conversation,
        IReadOnlyCollection<Id<UserProfileMarker>> initialParticipantUserIds,
        CancellationToken cancellationToken = default);

    Task<ConversationV2?> GetDuetByUserIdsAsync(
        Id<UserProfileMarker> firstUserId,
        Id<UserProfileMarker> secondUserId,
        CancellationToken cancellationToken = default);
}

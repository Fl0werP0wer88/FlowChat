using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationParticipantWriteRepository : IWriteRepository<ConversationParticipant>
{
    Task<ConversationParticipant?> GetActiveAsync(
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConversationParticipant>> GetActiveByUserIdsAsync(
        Id<ConversationV2> conversationId,
        IReadOnlyCollection<Id<UserProfileMarker>> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConversationParticipant>> GetActiveByConversationIdAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConversationParticipant>> GetHiddenByConversationIdAsync(
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> excludedUserId,
        CancellationToken cancellationToken = default);
}

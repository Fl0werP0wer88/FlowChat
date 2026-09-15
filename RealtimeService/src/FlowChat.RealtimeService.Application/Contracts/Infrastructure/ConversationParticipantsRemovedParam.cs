namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record ConversationParticipantsRemovedParam(
    Guid ConversationId,
    int ConversationType,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    int ParticipantCount,
    int MembershipRevision,
    IReadOnlyCollection<Guid> RecipientUserIds);

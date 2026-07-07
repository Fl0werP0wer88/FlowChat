namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record GroupConversationParticipantsRemovedParam(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

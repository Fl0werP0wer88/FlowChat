namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record ConversationParticipantsAddedParam(
    Guid ConversationId,
    int ConversationType,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    IReadOnlyCollection<Guid> RecipientUserIds);

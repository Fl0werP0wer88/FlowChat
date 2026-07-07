namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record GroupConversationParticipantsAddedParam(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

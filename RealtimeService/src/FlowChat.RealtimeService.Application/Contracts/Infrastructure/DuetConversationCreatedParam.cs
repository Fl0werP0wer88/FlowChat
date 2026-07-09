namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record DuetConversationCreatedParam(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

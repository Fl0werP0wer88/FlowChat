namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record GroupConversationChangedParam(
    Guid ConversationId,
    int Type,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds);

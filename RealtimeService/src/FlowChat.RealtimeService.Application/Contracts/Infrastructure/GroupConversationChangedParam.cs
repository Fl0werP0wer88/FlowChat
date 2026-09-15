namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record GroupConversationChangedParam(
    Guid ConversationId,
    int Type,
    string? Name,
    IReadOnlyCollection<Guid> ParticipantUserIds);

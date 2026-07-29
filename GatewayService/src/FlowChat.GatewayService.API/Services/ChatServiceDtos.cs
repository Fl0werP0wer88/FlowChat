namespace FlowChat.GatewayService.Api.Services;

public sealed record DuetConversationClientDto(
    Guid ConversationId,
    IReadOnlyCollection<ConversationParticipantClientDto> Participants);

public sealed record GroupConversationClientDto(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ConversationParticipantClientDto> Participants);

public sealed record ConversationParticipantClientDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId);

public sealed record ConversationMessagesRangeClientDto(
    IReadOnlyCollection<ChatMessageClientDto> Items,
    long StartSequenceNum,
    long EndSequenceNum,
    long CurrentSequenceNum,
    bool HasMore);

public sealed record ChatMessageClientDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc,
    long SequenceNum);

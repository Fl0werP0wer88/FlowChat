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

public sealed record ChatMessagesClientDto(
    IReadOnlyCollection<ChatMessageClientDto> Items,
    DateTimeOffset? NextBeforeSentAtUtc,
    Guid? NextBeforeMessageId,
    bool HasMore);

public sealed record ChatMessageClientDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc);

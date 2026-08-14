namespace FlowChat.Core.Messaging.ChatService.ReadModels;

public sealed record ConversationParticipantReadModelV2
{
    public required Guid ParticipantId { get; init; }
    public required Guid ConversationId { get; init; }
    public required int ConversationType { get; init; }
    public required Guid UserId { get; init; }
    public Guid? DuetPartnerUserId { get; init; }
    public string? DisplayName { get; init; }
    public required bool IsBlocked { get; init; }
    public required bool IsMuted { get; init; }
    public required bool IsHidden { get; init; }
    public required DateTimeOffset JoinedAtUtc { get; init; }
    public required long LastReadMessageSequenceNum { get; init; }
}

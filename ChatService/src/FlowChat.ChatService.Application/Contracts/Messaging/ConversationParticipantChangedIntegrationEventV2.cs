using FlowChat.Core.Messaging;

namespace FlowChat.ChatService.Application.Contracts.Messaging;
//Review2-3: Wydaje mi się że ten event być emitowany za pośrednictwem ProjectionIntegrationEvent<TValue> gdzie TValue będzie odpowiednim typem dla REadModelu. Trzeba by zmodyfikowac emitujący go procesor .Oceń ten pomysł.
public sealed record ConversationParticipantChangedIntegrationEventV2 : IntegrationEvent
{
    public required Guid ParticipantId { get; init; }
    public required Guid ConversationId { get; init; }
    public required Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public required bool IsBlocked { get; init; }
    public required bool IsMuted { get; init; }
    public required bool IsHidden { get; init; }
    public required DateTimeOffset JoinedAtUtc { get; init; }
    public required long LastReadMessageSequenceNum { get; init; }
    public required OperationType Operation { get; init; }
    public required int Version { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset ModifiedAtUtc { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
}

using FlowChat.Core.Messaging;

namespace FlowChat.ChatService.Application.Contracts.Messaging;

//Review2-2: Wydaje mi się że ten event być emitowany za pośrednictwem ProjectionIntegrationEvent<TValue> gdzie TValue będzie odpowiednim typem dla REadModelu. Trzeba by zmodyfikowac emitujący go procesor. Oceń ten pomysł.
public sealed record ConversationChangedIntegrationEventV2 : IntegrationEvent
{
    public required Guid ConversationId { get; init; }
    public required int ConversationType { get; init; }
    public string? Name { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required OperationType Operation { get; init; }
    public required int Version { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset ModifiedAtUtc { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
}

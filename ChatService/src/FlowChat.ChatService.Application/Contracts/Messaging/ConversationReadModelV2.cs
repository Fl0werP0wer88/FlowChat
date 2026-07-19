namespace FlowChat.ChatService.Application.Contracts.Messaging;

public sealed record ConversationReadModelV2
{
    public required Guid ConversationId { get; init; }
    public required int ConversationType { get; init; }
    public string? Name { get; init; }
    public required Guid CreatedByUserId { get; init; }
}

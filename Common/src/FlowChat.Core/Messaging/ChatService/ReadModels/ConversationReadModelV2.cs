namespace FlowChat.Core.Messaging.ChatService.ReadModels;

public sealed record ConversationReadModelV2
{
    public required Guid ConversationId { get; init; }
    public required int ConversationType { get; init; }
    public string? Name { get; init; }
    public required Guid CreatedByUserId { get; init; }
}

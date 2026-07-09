namespace FlowChat.Core.Messaging.ChatService.ReadModels;

public sealed record DuetConversationReadModel
{
    public required Guid ConversationId { get; init; }
    public required Guid FirstUserId { get; init; }
    public required Guid SecondUserId { get; init; }
}

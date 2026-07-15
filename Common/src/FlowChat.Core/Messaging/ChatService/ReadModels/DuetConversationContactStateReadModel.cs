namespace FlowChat.Core.Messaging.ChatService.ReadModels;

public sealed record DuetConversationContactStateReadModel
{
    public required Guid ConversationId { get; init; }
    public required Guid FirstUserId { get; init; }
    public required Guid SecondUserId { get; init; }
    public required bool FirstUserBlockedSecondUser { get; init; }
    public required bool SecondUserBlockedFirstUser { get; init; }
}

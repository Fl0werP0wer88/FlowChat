namespace FlowChat.Core.Messaging.ChatService.ReadModels;

public sealed record DuetConversationMembershipReadModel
{
    public required Guid ConversationId { get; init; }
    public required Guid FirstUserId { get; init; }
    public required Guid SecondUserId { get; init; }
    public required int ConversationMembershipRevision { get; init; }
}

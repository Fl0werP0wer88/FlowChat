namespace FlowChat.Core.Messaging.ChatService.ReadModels;

public sealed record GroupConversationMembershipReadModel
{
    public required Guid ConversationId { get; init; }
    public required Guid ParticipantUserId { get; init; }
}

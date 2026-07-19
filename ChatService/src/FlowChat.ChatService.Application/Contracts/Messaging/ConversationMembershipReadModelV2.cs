namespace FlowChat.ChatService.Application.Contracts.Messaging;

public sealed record ConversationMembershipReadModelV2
{
    public required Guid ConversationId { get; init; }
    public required Guid ParticipantUserId { get; init; }
}

namespace FlowChat.ChatService.Api.Features.ChatMessages.SendChatMessage;

public sealed class SendChatMessageRequest
{
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? Text { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}

using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.SendChatMessage;

public sealed class SendChatMessageRequest : IServiceInput
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? Text { get; init; }
}

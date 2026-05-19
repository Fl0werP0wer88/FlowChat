using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
}

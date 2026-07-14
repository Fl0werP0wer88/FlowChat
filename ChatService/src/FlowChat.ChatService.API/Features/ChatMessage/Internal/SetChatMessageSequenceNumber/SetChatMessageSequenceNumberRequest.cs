using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
}

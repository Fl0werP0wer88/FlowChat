using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.ProcessChatMessage;

public sealed class ProcessChatMessageRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
}

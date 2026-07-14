using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberResponse : IServiceOutput
{
    public long SequenceNum { get; init; }
}

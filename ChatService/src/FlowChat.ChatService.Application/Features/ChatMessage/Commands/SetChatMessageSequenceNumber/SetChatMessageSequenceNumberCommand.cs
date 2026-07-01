using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed record SetChatMessageSequenceNumberCommand(
    Guid MessageId,
    Guid ConversationId) : ICommand<long>;

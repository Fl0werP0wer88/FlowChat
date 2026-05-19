using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed record MarkChatMessageAsDeliveredCommand(
    Guid MessageId,
    Guid ConversationId) : ICommand<Unit>;

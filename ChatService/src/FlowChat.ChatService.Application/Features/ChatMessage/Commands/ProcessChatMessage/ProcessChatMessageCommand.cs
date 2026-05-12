using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.ProcessChatMessage;

public sealed record ProcessChatMessageCommand(
    Guid MessageId,
    Guid ConversationId) : ICommand<Unit>;

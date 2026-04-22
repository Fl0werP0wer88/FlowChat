using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed record SendChatMessageCommand(
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text) : ICommand<Guid>;


using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed record SendChatMessageCommandV2(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string? Text) : ICommand<SendChatMessageCommandResultV2>;
